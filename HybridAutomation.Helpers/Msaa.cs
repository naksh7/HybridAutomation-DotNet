using Accessibility;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridAutomation.Helpers
{
#pragma warning disable CA1416
    /// <summary>
    /// Lightweight MSAA (Microsoft Active Accessibility) helper to locate elements using a very small XPath-like syntax.
    /// Supports patterns such as: //window[@Name='Main Window']/editable_text[@Name='Field Name']
    /// Only @Name attribute is currently supported. Roles map to MSAA roles (window, editable_text, etc.).
    /// The descendant (second node) may be a direct child or any deeper descendant of the window node.
    /// </summary>
    public class Msaa : IDisposable
    {
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Default = new();
            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private static readonly Guid IID_IAccessible = new("618736E0-3C3D-11CF-810C-00AA00389B71");
        // Added: cache for window accessibles to avoid repeated EnumWindows lookups
        private static readonly object _windowCacheLock = new();
        private static readonly Dictionary<string, IAccessible?> _windowCache = new(StringComparer.OrdinalIgnoreCase);

        // Dictionary to handle common role name variations across different machines/OS versions
        private static readonly Dictionary<string, string> _roleNameMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["check_box"] = "checkbox",
            ["checkbox"] = "checkbox",
            ["check box"] = "checkbox",
            ["push_button"] = "button",
            ["pushbutton"] = "button",
            ["push button"] = "button",
            ["editable_text"] = "edit",
            ["editabletext"] = "edit",
            ["editable text"] = "edit",
            ["text_field"] = "edit",
            ["textfield"] = "edit",
            ["text field"] = "edit"
        };

        private bool _disposed = false;

        /// <summary>
        /// Object id for client area (OBJID_CLIENT) used with AccessibleObjectFromWindow.
        /// </summary>
        private const uint OBJID_CLIENT = 0xFFFFFFFC; // (uint)OBJID.CLIENT per WinUser.h

        /// <summary>
        /// Object id for a window (OBJID_WINDOW) used with AccessibleObjectFromWindow.
        /// </summary>
        private const uint OBJID_WINDOW = 0x00000000;

        /// <summary>
        /// Enumerates top-level windows by calling a callback for each window handle.
        /// Maps to the native EnumWindows Win32 API.
        /// </summary>
        [DllImport("user32.dll", SetLastError = false)] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Delegate type used as the callback for EnumWindows. The callback receives a window handle and lParam.
        /// Return true to continue enumeration or false to stop.
        /// </summary>
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Retrieves an IAccessible COM object for the specified window handle and object id.
        /// Maps to the oleacc AccessibleObjectFromWindow function.
        /// </summary>
        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid, [In, Out, MarshalAs(UnmanagedType.Interface)] ref IAccessible? ppvObject);

        /// <summary>
        /// Retrieves child accessibles for a container accessible using the oleacc AccessibleChildren API.
        /// </summary>
        [DllImport("oleacc.dll")] private static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren, ref int pcObtained);

        /// <summary>
        /// Gets a human-readable role string for the numeric role id using the oleacc GetRoleTextW API.
        /// </summary>
        [DllImport("oleacc.dll", CharSet = CharSet.Unicode)] private static extern uint GetRoleTextW(uint dwRole, StringBuilder lpszRole, uint cchRoleMax);

        /// <summary>
        /// Gets the length of the window title text for a given window handle (wide-char).
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLengthW(IntPtr hWnd);

        /// <summary>
        /// Retrieves the window title text for a given window handle (wide-char).
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Determines whether the specified window is visible. Maps to the Win32 IsWindowVisible API.
        /// </summary>
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Returns a handle to the desktop window. Maps to the Win32 GetDesktopWindow API.
        /// </summary>
        [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// Safely gets the parent IAccessible of the provided accessible instance without throwing.
        /// </summary>
        /// <param name="acc">Accessible instance whose parent should be retrieved.</param>
        /// <returns>Parent IAccessible or null if unavailable.</returns>
        private IAccessible? SafeGetParent(IAccessible acc) { try { return acc.accParent as IAccessible; } catch { return null; } }

        /// <summary>
        /// Safely retrieves the Name property for the given accessible and child identifier without throwing.
        /// </summary>
        /// <param name="acc">Accessible instance.</param>
        /// <param name="childId">Child id (0 for object itself).</param>
        /// <returns>Name string or null if not accessible.</returns>
        private string? SafeGetName(IAccessible acc, object childId) { try { return acc.get_accName(childId); } catch { return null; } }

        /// <summary>
        /// Safely retrieves the Value property for the given accessible and child identifier without throwing.
        /// </summary>
        /// <param name="acc">Accessible instance.</param>
        /// <param name="childId">Child id (0 for object itself).</param>
        /// <returns>Value string or null if not accessible.</returns>
        private string? SafeGetValue(IAccessible acc, object childId) { try { return acc.get_accValue(childId); } catch { return null; } }

        /// <summary>
        /// Safely retrieves the default action string for the accessible if available.
        /// </summary>
        /// <param name="acc">Accessible instance.</param>
        /// <returns>Default action string or null if not available.</returns>
        private string? SafeGetDefaultAction(IAccessible acc) { try { return acc.get_accDefaultAction(0); } catch { return null; } }

        /// <summary>
        /// Returns a human-friendly role name for the provided IAccessible object.
        /// Tries to interpret numeric role values and falls back to the object's ToString when necessary.
        /// </summary>
        /// <param name="acc">Accessible instance whose role is required.</param>
        /// <returns>Role name string for the accessible.</returns>
        private string GetRoleName(IAccessible acc)
        {
            try
            {
                object roleObj = acc.get_accRole(0);
                if (roleObj is int i) return RoleText((uint)i);
                if (roleObj is uint ui) return RoleText(ui);
                if (roleObj is short s) return RoleText((uint)s);
                return roleObj.ToString() ?? "Unknown";
            }
            catch { return "Unknown"; }
        }

        /// <summary>
        /// Converts a numeric MSAA role identifier into a role text string (spaces replaced with underscores).
        /// </summary>
        /// <param name="role">Numeric role identifier (from get_accRole).</param>
        /// <returns>Role text string.</returns>
        private string RoleText(uint role)
        {
            var sb = new StringBuilder(64);
            uint len = GetRoleTextW(role, sb, (uint)sb.Capacity);
            if (len > 0) return sb.ToString().Replace(' ', '_');
            return "Role" + role;
        }

        /// <summary>
        /// Normalizes a role name for comparison by converting to lowercase and replacing spaces/underscores consistently.
        /// This handles variations where different machines might return "check box" vs "checkbox" or "check_box".
        /// </summary>
        /// <param name="roleName">The role name to normalize</param>
        /// <returns>Normalized role name for comparison</returns>
        private string NormalizeRoleName(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return string.Empty;

            // Convert to lowercase and replace spaces and underscores with a consistent separator
            string normalized = roleName.ToLowerInvariant().Replace(" ", "_").Replace("__", "_");

            // Check if we have a mapping for this role name
            if (_roleNameMappings.TryGetValue(normalized, out string? mappedRole))
            {
                return mappedRole;
            }

            // Also check the original role name without normalization in case it's already in our mapping
            if (_roleNameMappings.TryGetValue(roleName, out mappedRole))
            {
                return mappedRole;
            }

            return normalized;
        }

        /// <summary>
        /// Checks if two role names match, handling variations in spacing and underscores.
        /// </summary>
        /// <param name="xPathRole">Role name from XPath (e.g., "checkbox")</param>
        /// <param name="actualRole">Actual role name from MSAA (e.g., "check box" or "check_box")</param>
        /// <returns>True if roles match after normalization</returns>
        private bool RoleNamesMatch(string xPathRole, string actualRole)
        {
            if (string.IsNullOrEmpty(xPathRole)) return true;
            if (string.IsNullOrEmpty(actualRole)) return false;

            string normalizedXPath = NormalizeRoleName(xPathRole);
            string normalizedActual = NormalizeRoleName(actualRole);

            return string.Equals(normalizedXPath, normalizedActual, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Locates an IAccessible element using a Selenium By XPath-style locator that this helper understands.
        /// The supported syntax is a simplified XPath using element roles and optional [@Name='...'] predicates.
        /// Example: //window[@Name='Main Window']/editable_text[@Name='Field Name'] 
        /// Role names are normalized to handle variations across different machines/OS versions where
        /// the same control might be reported as "checkbox" vs "check box" vs "check_box", etc.
        /// Common variations like editable_text/edit, push_button/button are automatically handled.
        /// </summary>
        /// <param name="xPath">Selenium By locator representing the MSAA XPath</param>
        /// <returns>IAccessible instance matching the locator or null if not found</returns>
        public IAccessible? GetElement(By xPath)
        {
            if (xPath == null)
                throw new ArgumentNullException(nameof(xPath), "XPath cannot be null.");

            // Normalize and split segments
            string trimmed = xPath.ToString().Replace("By.XPath: ", "").Trim('/');
            // Remove leading // or leading /
            while (trimmed.StartsWith("//") || trimmed.StartsWith("/"))
                trimmed = trimmed.TrimStart('/');

            var rawSegments = trimmed.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var segments = new List<(string role, string? name, int? index)>();
            // Support optional [@Name='...'] and optional numeric index like [2] or [@Index='2'] after the segment
            var segRegex = new Regex("^(?<role>\\w+)(\\[@Name='(?<name>[^']*)'\\])?(\\[@Index='(?<index2>\\d+)'\\])?(\\[(?<index>\\d+)\\])?", RegexOptions.Compiled);

            foreach (var raw in rawSegments)
            {
                var m = segRegex.Match(raw.Trim());
                if (!m.Success)
                    throw new ArgumentException($"Invalid xpath segment: {raw}");
                var role = m.Groups["role"].Value;
                var name = m.Groups["name"].Success ? m.Groups["name"].Value : null;
                int? index = null;
                if (m.Groups["index"].Success && int.TryParse(m.Groups["index"].Value, out var idx)) index = idx;
                else if (m.Groups["index2"].Success && int.TryParse(m.Groups["index2"].Value, out var idx2)) index = idx2;
                segments.Add((role, name, index));
            }

            if (segments.Count == 0)
                throw new ArgumentException("XPath did not contain any segments.");

            // Determine root
            int startIndex = 0;
            IAccessible? current = null;
            var first = segments[0];

            //New Code to handle the root element
            if (string.Equals(first.role, "window", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(first.name))
                    current = GetDesktopAccessible();
                else
                    current = GetWindowAccessible(first.name);

                if (current == null)
                    current = GetDesktopAccessible();
                startIndex = 1;
            }

            //Commented old code to handle the root element
            //if (string.Equals(first.role, "window", StringComparison.OrdinalIgnoreCase))
            //{
            //    if (string.IsNullOrWhiteSpace(first.name))
            //        current = GetDesktopAccessible();
            //    else
            //        current = GetWindowAccessible(first.name);

            //    //current = GetDesktopAccessible();
            //    startIndex = 1;
            //}
            //else if (string.Equals(first.role, "desktop", StringComparison.OrdinalIgnoreCase))
            //{
            //    current = GetDesktopAccessible();
            //    startIndex = 1;
            //}
            //else
            //{
            //    // default to desktop as root and start from first segment
            //    current = GetDesktopAccessible();
            //    startIndex = 0;
            //}

            if (current == null)
                throw new InvalidOperationException($"Unable to find window {first} for provided XPath : {xPath}");

            if (startIndex >= segments.Count) return current;

            var visited = new HashSet<IAccessible>(ReferenceEqualityComparer<IAccessible>.Default);

            for (int idx = startIndex; idx < segments.Count; idx++)
            {
                var (role, name, index) = segments[idx];

                // predicate to match nodes
                Func<IAccessible, bool> predicate = e =>
                {
                    if (visited.Contains(e)) return false;
                    visited.Add(e);
                    string? n = SafeGetName(e, 0);
                    string r = role == null ? string.Empty : GetRoleName(e);
                    bool nameOk = string.IsNullOrEmpty(name) || (n != null && string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                    bool roleOk = RoleNamesMatch(role, r);
                    return nameOk && roleOk;
                };

                IAccessible? found = null;

                if (index.HasValue)
                {
                    // collect all matching nodes in subtree in traversal order and pick the requested index (1-based)
                    var matches = new List<IAccessible>();
                    try { FindAllInSubtree(current, predicate, matches, index.Value); } catch { }
                    if (matches.Count >= index.Value)
                    {
                        found = matches[index.Value - 1];
                    }
                }
                else
                {
                    found = FindElementInSubtree(current, predicate);
                }

                if (found == null)
                    throw new ElementNotVisibleException($"Element with Xpath ='{xPath}' not found under the {first} window.");

                current = found;
                visited.Clear(); // reset visited for next stage to allow searching under new subtree
            }

            return current;
        }

        /// <summary>
        /// Helper to collect all accessibles that satisfy predicate in subtree rooted at 'root'.
        /// Stops early when maxCount is reached (if > 0).
        /// </summary>
        private void FindAllInSubtree(IAccessible root, Func<IAccessible, bool> predicate, List<IAccessible> results, int maxCount = 0)
        {
            try
            {
                // stack-based DFS to avoid deep recursion
                var stack = new Stack<IAccessible>();
                var visited = new HashSet<IAccessible>(ReferenceEqualityComparer<IAccessible>.Default);
                stack.Push(root);
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (node == null) continue;
                    if (visited.Contains(node)) continue;
                    visited.Add(node);

                    if (predicate(node)) results.Add(node);
                    if (maxCount > 0 && results.Count >= maxCount) return;

                    try
                    {
                        foreach (var child in EnumerateChildren(node))
                        {
                            if (!visited.Contains(child)) stack.Push(child);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Retrieves the desktop IAccessible root object using the Win32 GetDesktopWindow and oleacc APIs.
        /// </summary>
        /// <returns>IAccessible instance representing the desktop or null if unavailable.</returns>
        private IAccessible? GetDesktopAccessible()
        {
            try
            {
                IntPtr desktop = GetDesktopWindow();
                if (desktop == IntPtr.Zero) return null;
                IAccessible? acc = null;
                Guid g = IID_IAccessible;
                int hr = AccessibleObjectFromWindow(desktop, OBJID_WINDOW, ref g, ref acc);
                return hr >= 0 ? acc : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Finds an accessible object for a top-level window by its title using EnumWindows.
        /// Results are cached to avoid repeated enumeration; stale COM objects are validated and released automatically.
        /// </summary>
        /// <param name="windowName">Exact window title to match (case-insensitive).</param>
        /// <returns>IAccessible for the located window or null if not found.</returns>
        private IAccessible? GetWindowAccessible(string windowName)
        {
            IAccessible? result = null;
            string target = windowName.Trim();

            // Check cache first to reuse previously discovered window accessibles
            try
            {
                lock (_windowCacheLock)
                {
                    if (_windowCache.TryGetValue(target, out var cached) && cached != null)
                    {
                        // Validate cached COM object by trying to access a property
                        try
                        {
                            // Accessing accChildCount will throw if COM object is no longer valid
                            var dummy = cached.accChildCount;
                            return cached;
                        }
                        catch
                        {
                            // Release invalid cached COM object, remove entry and fall through to re-enumerate
                            // Validate platform compatibility
                            try { Marshal.FinalReleaseComObject(cached); } catch { }
                            // Validate platform compatibility
                            _windowCache.Remove(target);
                        }
                    }
                }
            }
            catch { /* ignore cache errors and continue */ }

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                int len = GetWindowTextLengthW(hWnd);
                if (len == 0) return true;
                var sb = new StringBuilder(len + 2);
                GetWindowTextW(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (string.Equals(title, target, StringComparison.OrdinalIgnoreCase))
                {
                    IAccessible? acc = null;
                    Guid g = IID_IAccessible;
                    int hr = AccessibleObjectFromWindow(hWnd, OBJID_CLIENT, ref g, ref acc);
                    if (hr < 0 || acc == null)
                    {
                        g = IID_IAccessible;
                        hr = AccessibleObjectFromWindow(hWnd, OBJID_WINDOW, ref g, ref acc);
                    }
                    result = acc; return false;
                }
                return true;
            }, IntPtr.Zero);

            // Cache the result for reuse
            try
            {
                if (result != null)
                {
                    lock (_windowCacheLock)
                    {
                        // If an existing different cached entry exists, release it first
                        if (_windowCache.TryGetValue(target, out var existing) && existing != null && !ReferenceEquals(existing, result))
                        {
                            try { Marshal.FinalReleaseComObject(existing); } catch { }
                        }
                        _windowCache[target] = result;
                    }
                }
            }
            catch { /* ignore cache set errors */ }

            return result;
        }

        /// <summary>
        /// Recursively searches the accessible subtree starting at the provided root and returns the first node that satisfies the predicate.
        /// </summary>
        /// <param name="root">Root accessible to start the search from.</param>
        /// <param name="predicate">Function that evaluates whether a node matches the search criteria.</param>
        /// <returns>First matching IAccessible or null if none found.</returns>
        private IAccessible? FindElementInSubtree(IAccessible root, Func<IAccessible, bool> predicate)
        {
            try
            {
                if (predicate(root)) return root;
                foreach (var child in EnumerateChildren(root))
                {
                    var found = FindElementInSubtree(child, predicate);
                    if (found != null)
                        return found;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Enumerates the child accessibles of the provided accessible. Handles simple children and child IDs returned by AccessibleChildren.
        /// </summary>
        /// <param name="acc">Accessible instance whose children should be enumerated.</param>
        /// <returns>IEnumerable of child IAccessible instances (may be empty).</returns>
        private IEnumerable<IAccessible> EnumerateChildren(IAccessible acc)
        {
            var results = new List<IAccessible>();
            int count; try { count = acc.accChildCount; } catch { return results; }
            object[] children = new object[count]; int obtained = 0;
            try { if (AccessibleChildren(acc, 0, count, children, ref obtained) != 0) obtained = 0; } catch { obtained = 0; }
            if (obtained > 0)
            {
                for (int i = 0; i < obtained; i++)
                {
                    var childObj = children[i];
                    if (childObj is IAccessible ia) results.Add(ia);
                    else if (childObj is int childId)
                    { try { object? o = acc.get_accChild(childId); if (o is IAccessible ia2) results.Add(ia2); } catch { } }
                }
                return results;
            }
            for (int i = 1; i <= count; i++)
            { try { object? childObj = acc.get_accChild(i); if (childObj is IAccessible childAcc) results.Add(childAcc); } catch { } }
            return results;
        }

        /// <summary>
        /// Gets text for the element identified by an XPath-style By locator using MSAA.
        /// </summary>
        /// <param name="xPath">Selenium By locator representing the MSAA XPath</param>
        /// <returns>Text value of the element or empty string if not found</returns>
        public string GetText(By xPath)
        {
            if (xPath == null) throw new ArgumentNullException(nameof(xPath));
            var element = GetElement(xPath);
            if (element == null) return string.Empty;
            return SafeGetValue(element, 0) ?? string.Empty;
        }

        /// <summary>
        /// Gets text for the element identified by a locator using MSAA.
        /// </summary>
        /// <param name="locator">IAccessible representing the MSAA XPath</param>
        /// <returns>Text value of the element or empty string if not found</returns>
        public string GetText(IAccessible locator)
        {
            if (locator == null) return string.Empty;
            return SafeGetValue(locator, 0) ?? string.Empty;
        }

        /// <summary>
        /// Clicks the element identified by the provided XPath-style By locator using MSAA.
        /// Resolves the IAccessible for the locator and performs a mouse click at its center.
        /// </summary>
        /// <param name="xPath">Selenium By locator (XPath) that maps to MSAA GetElement</param>
        public void Click(By xPath)
        {
            if (xPath == null) throw new ArgumentNullException(nameof(xPath));
            try
            {
                var element = GetElement(xPath);
                int left = 0, top = 0, width = 0, height = 0;
                element?.accLocation(out left, out top, out width, out height, 0);
                int x = left + width / 2;
                int y = top + height / 2;
                Utilities.Input.InputMoveMouseTo(x, y);
                Utilities.Input.InputLeftClick();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClick failed for locator: {xPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks the element identified by the provided by IAccessible using MSAA.
        /// Resolves the IAccessible for the locator and performs a mouse click at its center.
        /// </summary>
        /// <param name="locator">IAccessible locator that maps to MSAA GetElement</param>
        public void Click(IAccessible locator)
        {
            if (locator == null) throw new ArgumentNullException(nameof(locator));
            try
            {
                int left = 0, top = 0, width = 0, height = 0;
                locator?.accLocation(out left, out top, out width, out height, 0);
                int x = left + width / 2;
                int y = top + height / 2;
                Utilities.Input.InputMoveMouseTo(x, y);
                Utilities.Input.InputLeftClick();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClick failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears existing text and enters new text into an MSAA-identified element using native input automation.
        /// </summary>
        /// <param name="locator">Selenium By locator representing the MSAA XPath</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetTextIn(By locator, string text)
        {
            try
            {
                Click(locator);
                Utilities.Input.ClearField();
                Utilities.Input.InputText(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetTextIn failed for locator: {locator} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears existing text and enters new text into an MSAA-identified element using native input automation.
        /// </summary>
        /// <param name="locator">Selenium By locator representing the MSAA XPath</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetTextIn(IAccessible element, string text)
        {
            try
            {
                Click(element);
                Utilities.Input.ClearField();
                Utilities.Input.InputText(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetTextIn failed for locator: {element} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sends special keyboard keys using native Win32 API input automation.
        /// </summary>
        /// <param name="key">Special key Unicode value (e.g., \uE007 for Enter, \uE004 for Tab)</param>
        public void SendKeysIn(string key)
        {
            switch (key.ToLower())
            {
                case "\uE007":
                    Utilities.Input.InputEnter();
                    break;
                case "\uE004":
                    Utilities.Input.InputTab();
                    break;
                case "\uE013":
                    Utilities.Input.InputArrowUp();
                    break;
                case "\uE015":
                    Utilities.Input.InputArrowDown();
                    break;
                case "\uE012":
                    Utilities.Input.InputArrowLeft();
                    break;
                case "\uE014":
                    Utilities.Input.InputArrowRight();
                    break;
            }
        }

        /// <summary>
        /// Waits until the element identified by the provided XPath-style locator is present and appears visible.
        /// Returns the IAccessible when found and visible, or null when timeout elapses.
        /// </summary>
        /// <param name="xPath">Selenium By locator representing the MSAA XPath</param>
        /// <param name="timeoutSeconds">Maximum wait time in seconds (default 30s)</param>
        /// <param name="pollIntervalMs">Polling interval in milliseconds (default 250ms)</param>
        public bool WaitForElementVisible(By xPath, int timeoutSeconds = 5)
        {
            if (xPath == null) throw new ArgumentNullException(nameof(xPath));
            if (timeoutSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            var sw = Stopwatch.StartNew();
            Exception? lastEx = null;

            while (sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var el = GetElement(xPath);
                    if (el != null)
                    {
                        try
                        {
                            // try to get location/size - if accessible and has area > 0 consider visible
                            int left = 0, top = 0, width = 0, height = 0;
                            el.accLocation(out left, out top, out width, out height, 0);
                            if (width > 0 || height > 0)
                                return true;
                        }
                        catch
                        {
                            // fallback: if it has a name or value, treat as visible
                            var name = SafeGetName(el, 0);
                            var value = SafeGetValue(el, 0);
                            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(value))
                                return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // store last exception and continue polling
                    lastEx = ex;
                }
            }

            // timed out
            throw new TimeoutException($"WaitForElementVisible timed out after {timeoutSeconds} seconds for locator: {xPath}", lastEx);
        }

        /// <summary>
        /// Safely releases a COM RCW if not null.
        /// </summary>
        /// <param name="comObj">COM object to release</param>
        private void ReleaseComObjectSafe(object? comObj)
        {
            if (comObj == null) return;
            try
            {
                Marshal.FinalReleaseComObject(comObj);
            }
            catch { /* ignore failures during release */ }
        }

        /// <summary>
        /// Dispose pattern - releases cached COM accessibles.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs dispose logic and releases any cached COM accessibles held in the class.
        /// </summary>
        /// <param name="disposing">Indicates whether managed resources should be disposed (true) or finalizer call (false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            // release cached COM objects
            try
            {
                lock (_windowCacheLock)
                {
                    foreach (var kvp in _windowCache.ToList())
                    {
                        if (kvp.Value != null)
                        {
                            ReleaseComObjectSafe(kvp.Value);
                        }
                    }
                    _windowCache.Clear();
                }
            }
            catch { }

            _disposed = true;
        }

        /// <summary>
        /// Selects a dropdown option by typing characters until the desired value is matched.
        /// </summary>
        /// <param name="locator">Dropdown element locator</param>
        /// <param name="text">Option text to match</param>
        /// <param name="codeToMatch">Specific code to match (optional, defaults to text parameter)</param>
        public void SelectDropdownByValue(By locator, string text, string codeToMatch = "")
        {
            if (string.IsNullOrEmpty(codeToMatch))
                codeToMatch = text;
            try
            {
                while (true)
                {
                    if (GetText(locator).Equals(codeToMatch))
                        break;
                    else
                        SetTextIn(locator, text[0].ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for locator: {locator} and value: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing characters until the desired value is matched.
        /// </summary>
        /// <param name="locator">Dropdown element locator</param>
        /// <param name="text">Option text to match</param>
        /// <param name="codeToMatch">Specific code to match (optional, defaults to text parameter)</param>
        public void SelectDropdownByValue(IAccessible locator, string text, string codeToMatch = "")
        {
            if (string.IsNullOrEmpty(codeToMatch))
                codeToMatch = text;
            try
            {
                while (true)
                {
                    if (GetText(locator).Equals(codeToMatch))
                        break;
                    else
                        SetTextIn(locator, text[0].ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for locator: {locator} and value: {text}\n{ex.StackTrace}", ex);
            }
        }
    }
}
