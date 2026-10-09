using Microsoft.Playwright;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Web browser automation using Microsoft Playwright with multi-browser support and centralized session management.
    /// Provides similar interface to Selenium with synchronous method calls and enhanced capabilities.
    /// 
    /// Enhanced with DriverManager integration for unified session management.
    /// </summary>
    public class Playwright
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;
        private PlaywrightDetails? PlaywrightDetails;

        private IPage GetPage()
        {
            return _page ?? throw new InvalidOperationException("Page is not initialized. Call InitializeDriver first.");
        }

        /// <summary>
        /// Initializes Playwright, browser, and creates a new page with specified browser type.
        /// Now enhanced with DriverManager integration option while maintaining full backward compatibility.
        /// </summary>
        /// <param name="browserType">Browser type: "chromium", "firefox", "webkit" (default: "chromium")</param>
        /// <param name="headless">Run browser in headless mode (default: false)</param>
        /// <param name="downloadPath">Download directory path (optional)</param>
        public void InitializeDriver(string browserType = "chromium", bool headless = false, string? downloadPath = null)
        {
            try
            {
                if (PlaywrightDetails != null)
                    CloseApp();
                PlaywrightDetails = Utilities.DriverManager.CreatePlaywrightDriver(browserType, headless, downloadPath);

                _playwright = PlaywrightDetails.PlaywrightInstance;
                _browser = PlaywrightDetails.BrowserInstance;
                _context = PlaywrightDetails.BrowserContextInstance;
                _page = PlaywrightDetails.PageInstance;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nInitializeDriver failed for browser: {browserType}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Navigates page to the specified URL.
        /// </summary>
        /// <param name="url">Target URL to navigate to</param>
        public void NavigateToURL(string url)
        {
            try
            {
                GetPage().GotoAsync(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nNavigateToURL failed for url: {url}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Reloads the current page and waits till DOM loads.
        /// </summary>
        public void RefreshPage()
        {
            try
            {
                GetPage().ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRefreshPage failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Opens the specified URL in a new browser tab and switches focus to it.
        /// </summary>
        /// <param name="url">URL to open in the new tab</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void OpenLinkInNewTab(string url, int timeoutInSeconds = 20)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                var newPage = _context.NewPageAsync().GetAwaiter().GetResult();
                newPage.GotoAsync(url, new PageGotoOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
                _page = newPage;
                GetPage().BringToFrontAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nOpenLinkInNewTab failed for url: {url}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Closes browser and terminates Playwright session with cleanup.
        /// </summary>
        public void CloseApp()
        {
            try
            {
                try
                {
                    try
                    {
                        if (_page != null) _page.CloseAsync().GetAwaiter().GetResult();
                    }
                    finally
                    {
                        try
                        {
                            if (_context != null) _context.CloseAsync().GetAwaiter().GetResult();
                        }
                        finally
                        {
                            if (_browser != null) _browser.CloseAsync().GetAwaiter().GetResult();
                        }
                    }
                }
                finally
                {
                    try
                    {
                        _playwright?.Dispose();
                    }
                    finally
                    {
                        if (PlaywrightDetails != null)
                        {
                            PlaywrightDetails.PageInstance = null;
                            PlaywrightDetails.BrowserContextInstance = null;
                            PlaywrightDetails.BrowserInstance = null;
                            PlaywrightDetails.PlaywrightInstance = null;
                        }

                        _page = null;
                        _context = null;
                        _browser = null;
                        _playwright = null;
                        PlaywrightDetails = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCloseApp failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks the specified element and handles the JavaScript dialog that may be triggered by the click.
        /// </summary>
        /// <param name="selector">The selector used to locate the element to click.</param>
        /// <param name="buttonAction">The action to perform on the dialog. Supported values are<c>OK</c>, <c>Accept</c>, <c>Cancel</c>, and <c>Dismiss</c></param>
        /// <param name="timeoutInSeconds">The maximum amount of time, in seconds, to wait for the element to be clickable.</param>       
        public void ClickAndHandleDialog(string selector, string buttonAction = "OK", int timeoutInSeconds = 20)
        {
            var page = GetPage();

            EventHandler<IDialog> dialogHandler = async (_, dialog) =>
            {
                if (buttonAction.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                    buttonAction.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    await dialog.AcceptAsync();
                }
                else if (buttonAction.Equals("Cancel", StringComparison.OrdinalIgnoreCase) ||
                         buttonAction.Equals("Dismiss", StringComparison.OrdinalIgnoreCase))
                {
                    await dialog.DismissAsync();
                }
                else
                {
                    throw new ArgumentException($"Unsupported dialog action: {buttonAction}", nameof(buttonAction));
                }
            };

            try
            {
                // Register the handler before clicking.
                page.Dialog += dialogHandler;

                // Use the existing Click method.
                Click(selector, timeoutInSeconds);
            }
            catch (Exception ex)
            {
                throw new Exception($"Click and dialog handling failed for selector: {selector}", ex);
            }
            finally
            {
                // Prevent duplicate handlers on later clicks.
                page.Dialog -= dialogHandler;
            }
        }

        /// <summary>
        /// Finds and returns web element using the specified selector.
        /// </summary>
        /// <param name="selector">CSS selector, XPath, or other supported selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        /// <returns>Located web element</returns>
        public IElementHandle GetElement(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var element = GetPage().WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
                if (element == null)
                {
                    throw new Exception($"Element not found for selector: {selector}");
                }
                return element;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElement failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds and returns all web elements matching the specified selector.
        /// </summary>
        /// <param name="selector">CSS selector, XPath, or other supported selector</param>
        /// <returns>List of all matching web elements</returns>
        public List<IElementHandle> GetElements(string selector)
        {
            try
            {
                var elements = GetPage().QuerySelectorAllAsync(selector).GetAwaiter().GetResult();
                return elements.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElements failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears existing text and enters new text into web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="text">Text to enter into the element</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void SetText(string selector, string text, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().FillAsync(selector, text, new PageFillOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetText failed for selector: {selector} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets visible text content from web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        /// <returns>Element's visible text content</returns>
        public string GetText(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var text = GetPage().TextContentAsync(selector, new PageTextContentOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetText failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears text content from input element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void ClearText(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().FillAsync(selector, "", new PageFillOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClearText failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sends keyboard input to element without clearing existing content.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="keys">Keys or text to send to the element</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void SendKeys(string selector, string keys, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().Locator(selector).PressSequentiallyAsync(keys, new LocatorPressSequentiallyOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSendKeys failed for selector: {selector} and keys: {keys}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets specified attribute value from web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="attributeName">Name of the attribute to retrieve</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        /// <returns>Attribute value or null if attribute doesn't exist</returns>
        public string? GetAttribute(string selector, string attributeName, int timeoutInSeconds = 20)
        {
            try
            {
                return GetPage().GetAttributeAsync(selector, attributeName, new PageGetAttributeOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAttribute failed for selector: {selector} and attribute: {attributeName}\n{ex.StackTrace}", ex);
            }
        }

        public string GetInputValue(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                return GetPage().InputValueAsync(selector, new PageInputValueOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetInputValue failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Counts the number of elements matching the specified selector.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <returns>Total number of matching elements found</returns>
        public int GetElementCount(string selector)
        {
            try
            {
                var elements = GetElements(selector);
                return elements.Count;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementCount failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to a different browser page/tab.
        /// </summary>
        /// <param name="pageIndex">Zero-based index of the page to switch to</param>
        public void SwitchToPage(int pageIndex)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                var pages = _context.Pages;
                if (pageIndex >= 0 && pageIndex < pages.Count)
                {
                    _page = pages[pageIndex];
                    GetPage().BringToFrontAsync().GetAwaiter().GetResult();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index {pageIndex} is out of range. Available pages: {pages.Count}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchToPage failed for pageIndex: {pageIndex}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to a browser page/tab by title match.
        /// </summary>
        /// <param name="title">Full or partial page title to match</param>
        public void SwitchPage(string title)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty.", nameof(title));
                }

                var pages = _context.Pages;
                foreach (var page in pages)
                {
                    var currentTitle = page.TitleAsync().GetAwaiter().GetResult();
                    if (currentTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
                    {
                        _page = page;
                        GetPage().BringToFrontAsync().GetAwaiter().GetResult();
                        return;
                    }
                }

                var availableTitles = pages.Select(p => p.TitleAsync().GetAwaiter().GetResult());
                throw new Exception($"No page found with title containing: {title}. Available titles: {string.Join(" | ", availableTitles)}");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchPage failed for title: {title}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to iframe or frame element within the page.
        /// </summary>
        /// <param name="selector">Frame element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        /// <returns>Frame object for further operations</returns>
        public IFrame SwitchToFrame(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var frameElement = GetPage().WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
                if (frameElement == null)
                {
                    throw new Exception($"Frame element not found for selector: {selector}");
                }

                var frame = frameElement.ContentFrameAsync().GetAwaiter().GetResult();
                if (frame == null)
                {
                    throw new Exception($"Could not get frame content for selector: {selector}");
                }

                return frame;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchToFrame failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs drag and drop operation between source and target elements.
        /// </summary>
        /// <param name="sourceSelector">Source element selector</param>
        /// <param name="targetSelector">Target element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void DragAndDrop(string sourceSelector, string targetSelector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().DragAndDropAsync(sourceSelector, targetSelector, new PageDragAndDropOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDragAndDrop failed from {sourceSelector} to {targetSelector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Moves mouse cursor to the specified element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void MoveToElement(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().HoverAsync(selector, new PageHoverOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nMoveToElement failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Scrolls page to bring element into view.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeout">Timeout in milliseconds (default: 30000)</param>
        public void ScrollToElement(string selector)
        {
            try
            {
                // Use JavaScript to scroll element into view
                GetPage().EvaluateAsync(@"(selector) => {
                    const element = document.querySelector(selector);
                    if (element) {
                        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }", selector).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nScrollToElement failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Maximizes the browser window to full screen.
        /// </summary>
        public void MaximizeWindow()
        {
            try
            {
                GetPage().SetViewportSizeAsync(1920, 1080).GetAwaiter().GetResult(); // Set to a common full-screen size
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nMaximizeWindow failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sets the browser window size.
        /// </summary>
        /// <param name="width">Window width in pixels</param>
        /// <param name="height">Window height in pixels</param>
        public void SetWindowSize(int width, int height)
        {
            try
            {
                GetPage().SetViewportSizeAsync(width, height).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetWindowSize failed with width: {width}, height: {height}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets collection of all available pages in the current browser context.
        /// </summary>
        /// <returns>List of page objects</returns>
        public List<IPage> GetPages()
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                return _context.Pages.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetPages failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Closes current page (not the entire session) and switches focus to the last remaining tab.
        /// </summary>
        public void ClosePage()
        {
            try
            {
                if (_page != null)
                {
                    GetPage().CloseAsync().GetAwaiter().GetResult();
                    _page = null;

                    if (_context != null && _context.Pages.Count > 0)
                    {
                        _page = _context.Pages.Last();
                        GetPage().BringToFrontAsync().GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClosePage failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Closes the browser tab whose title contains the specified text.
        /// If the closed tab was the active page, focus is moved to the last remaining tab.
        /// </summary>
        /// <param name="title">Full or partial page title to match</param>
        public void CloseTab(string title)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty.", nameof(title));
                }

                var pages = _context.Pages;
                IPage? pageToClose = null;
                foreach (var page in pages)
                {
                    var currentTitle = page.TitleAsync().GetAwaiter().GetResult();
                    if (currentTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
                    {
                        pageToClose = page;
                        break;
                    }
                }

                if (pageToClose == null)
                {
                    var availableTitles = pages.Select(p => p.TitleAsync().GetAwaiter().GetResult());
                    throw new Exception($"No tab found with title containing: {title}. Available titles: {string.Join(" | ", availableTitles)}");
                }

                var wasActivePage = pageToClose == _page;
                pageToClose.CloseAsync().GetAwaiter().GetResult();

                if (wasActivePage)
                {
                    _page = _context.Pages.Count > 0 ? _context.Pages.Last() : null;
                    _page?.BringToFrontAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCloseTab failed for title: {title}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for page to contain specified text.
        /// </summary>
        /// <param name="text">Expected text content to wait for</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds (default: 30)</param>
        public void WaitForText(string text, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().WaitForSelectorAsync($"text={text}", new PageWaitForSelectorOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForText failed for text: {text} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Highlights element with red border for debugging purposes.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeout">Timeout in milliseconds (default: 30000)</param>
        public void HighlightElement(string selector)
        {
            try
            {
                GetPage().EvaluateAsync(@"(selector) => {
                    const element = document.querySelector(selector);
                    if (element) {
                        element.style.border = '3px solid red';
                    }
                }", selector).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nHighlightElement failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Hovers mouse cursor over element (same as MoveToElement).
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void HoverOverElement(string selector, int timeoutInSeconds = 20)
        {
            MoveToElement(selector, timeoutInSeconds);
        }

        /// <summary>
        /// Scrolls within page by pixel offset.
        /// </summary>
        /// <param name="x">Horizontal scroll offset in pixels</param>
        /// <param name="y">Vertical scroll offset in pixels</param>
        public void Scroll(int x, int y)
        {
            try
            {
                GetPage().EvaluateAsync($"window.scrollBy({x}, {y})").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nScroll failed with x: {x}, y: {y}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Pauses execution for specified duration.
        /// </summary>
        /// <param name="timeoutInSeconds">Wait duration in seconds (default: 10)</param>
        public void WaitForAppIdle(int timeoutInSeconds = 20)
        {
            try
            {
                System.Threading.Thread.Sleep(timeoutInSeconds * 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForAppIdle failed with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code in the page context.
        /// </summary>
        /// <param name="script">JavaScript code to execute</param>
        /// <param name="args">Arguments to pass to the script</param>
        /// <returns>Script execution result</returns>
        public object? ExecuteJavaScript(string script, params object[] args)
        {
            try
            {
                return GetPage().EvaluateAsync(script, args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nExecuteJavaScript failed for script: {script}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the current page URL.
        /// </summary>
        /// <returns>Current page URL</returns>
        public string GetCurrentUrl()
        {
            try
            {
                return GetPage().Url;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetCurrentUrl failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the current page title.
        /// </summary>
        /// <returns>Current page title</returns>
        public string GetTitle()
        {
            try
            {
                return GetPage().TitleAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetTitle failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs left-click action on web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void Click(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().ClickAsync(selector, new PageClickOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClick failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs double-click action on web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void DoubleClick(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().DblClickAsync(selector, new PageDblClickOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDoubleClick failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs right-click (context menu) action on web element.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void RightClick(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().ClickAsync(selector, new PageClickOptions { Button = MouseButton.Right, Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRightClick failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects dropdown option by visible text.
        /// </summary>
        /// <param name="selector">Dropdown element selector</param>
        /// <param name="text">Visible text of the option to select</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void SelectDropdownByText(string selector, string text, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().SelectOptionAsync(selector, new SelectOptionValue { Label = text },
                    new PageSelectOptionOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByText failed for selector: {selector} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects dropdown option by index position.
        /// </summary>
        /// <param name="selector">Dropdown element selector</param>
        /// <param name="index">Zero-based index of the option to select</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void SelectDropdownByIndex(string selector, int index, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().SelectOptionAsync(selector, new SelectOptionValue { Index = index },
                    new PageSelectOptionOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByIndex failed for selector: {selector} and index: {index}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects dropdown option by value attribute.
        /// </summary>
        /// <param name="selector">Dropdown element selector</param>
        /// <param name="value">Value attribute of the option to select</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void SelectDropdownByValue(string selector, string value, int timeoutInSeconds = 20)
        {
            try
            {
                GetPage().SelectOptionAsync(selector, new SelectOptionValue { Value = value },
                    new PageSelectOptionOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for selector: {selector} and value: {value}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for element to become visible.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds (default: 10)</param>
        /// <returns>Located web element when visible</returns>
        public IElementHandle WaitForElementVisible(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var element = GetPage().WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutInSeconds * 1000
                }).GetAwaiter().GetResult();

                if (element == null)
                {
                    throw new Exception($"Element not visible for selector: {selector}");
                }
                return element;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementVisible failed for selector: {selector} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for element to become clickable (attached and visible).
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds (default: 10)</param>
        /// <returns>Clickable web element when condition is met</returns>
        public IElementHandle WaitForElementClickable(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var element = GetPage().WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutInSeconds * 1000
                }).GetAwaiter().GetResult();

                if (element == null)
                {
                    throw new Exception($"Element not clickable for selector: {selector}");
                }
                return element;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementClickable failed for selector: {selector} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for an element to be attached to the DOM without throwing exceptions.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds (default: 10)</param>        
        public bool WaitForElementExists(string selector, int timeoutInSeconds = 20)
        {
            try
            {
                var element = GetPage().WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = timeoutInSeconds * 1000
                }).GetAwaiter().GetResult();

                if (element == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if element exists in DOM without throwing exceptions.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <returns>True if element is present, false otherwise</returns>
        public bool IsElementPresent(string selector)
        {
            try
            {
                if (_page == null) return false;
                var element = GetPage().QuerySelectorAsync(selector).GetAwaiter().GetResult();
                return element != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if element is enabled for user interaction without throwing exceptions.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <returns>True if element is enabled, false otherwise</returns>
        public bool IsElementEnabled(string selector)
        {
            try
            {
                return GetPage().IsEnabledAsync(selector).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        public bool IsElementChecked(string selector)
        {
            try
            {
                return GetPage().IsCheckedAsync(selector).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if element is visible on page without throwing exceptions.
        /// </summary>
        /// <param name="selector">Element selector</param>
        /// <returns>True if element is displayed, false otherwise</returns>
        public bool IsElementDisplayed(string selector)
        {
            try
            {
                if (_page == null) return false;
                return GetPage().IsVisibleAsync(selector).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clicks an element and waits for the resulting file download to complete, then saves it to the specified path.
        /// Handles downloads triggered via new tabs (e.g. direct-download links that open in a new page).
        /// </summary>
        /// <param name="selector">Element selector to click</param>
        /// <param name="saveAsPath">Full path (including filename) where the downloaded file should be saved</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 5)</param>
        public void ClickAndWaitForDownload(string selector, string saveAsPath, int timeoutInSeconds = 20)
        {
            try
            {
                var download = GetPage().RunAndWaitForDownloadAsync(async () =>
                {
                    await GetPage().ClickAsync(selector, new PageClickOptions { Timeout = timeoutInSeconds * 1000 });
                }, new PageRunAndWaitForDownloadOptions { Timeout = timeoutInSeconds * 1000 }).GetAwaiter().GetResult();

                download.SaveAsAsync(saveAsPath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClickAndWaitForDownload failed for selector: {selector}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets a Cookie request header, including HttpOnly cookies, for the specified URL or current page.
        /// </summary>
        /// <param name="url">Target request URL. Defaults to the current page URL.</param>
        /// <returns>Applicable cookies formatted as name=value pairs, or an empty string if none exist.</returns>
        public string GetCookieHeader(string? url = null)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Browser context is not initialized. Call InitializeDriver first.");
                }

                var targetUrl = url ?? GetPage().Url;
                if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("An absolute HTTP or HTTPS URL is required.", nameof(url));
                }

                var cookies = _context.CookiesAsync(new[] { targetUrl }).GetAwaiter().GetResult();
                return string.Join("; ", cookies.Select(cookie => $"{cookie.Name}={cookie.Value}"));
            }
            catch (Exception ex)
            {
                throw new Exception("GetCookieHeader failed.", ex);
            }
        }
    }
}
