using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Desktop application automation using WinAppDriver with centralized session management.
    /// </summary>
    public class WinApp
    {
        public WindowsDriver<WindowsElement>? Driver = null;

        /// <summary>
        /// Initializes a new WinAppDriver session for desktop application automation.
        /// </summary>
        /// <param name="applicationPath">Full path to the target application executable</param>
        /// <param name="applicationName">Target application process name for session attachment</param>
        /// <param name="winAppDriverExePath">Full path to WinAppDriver.exe</param>
        /// <param name="winAppDriverURI">WinAppDriver service URI (e.g., http://127.0.0.1:4723)</param>
        /// <param name="debug">Debug mode flag - skips application launch when true (default: false)</param>
        public void InitializeSession(string applicationPath, string applicationName, string winAppDriverExePath, string winAppDriverURI, bool debug = false)
        {
            try
            {
                Driver = Utilities.DriverManager.CreateSession(applicationPath, applicationName, winAppDriverExePath, winAppDriverURI, debug);

                if (Driver == null)
                {
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nInitializeSession failed with winAppDriverExePath: {winAppDriverExePath}, winAppDriverURI: {winAppDriverURI}, applicationName: {applicationName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Re-establishes connection to an existing desktop application session.
        /// </summary>
        /// <param name="applicationPath">Full path to the target application executable</param>
        /// <param name="applicationName">Target application process name for session attachment</param>
        /// <param name="winAppDriverExePath">Full path to WinAppDriver.exe</param>
        /// <param name="winAppDriverURI">WinAppDriver service URI (e.g., http://127.0.0.1:4723)</param>
        /// <param name="debug">Debug mode flag - skips application launch when true (default: false)</param>
        public void ReattachSession(string applicationPath, string applicationName, string winAppDriverExePath, string winAppDriverURI, bool debug = false)
        {
            try
            {
                Driver = Utilities.DriverManager.CreateSession(applicationPath, applicationName, winAppDriverExePath, winAppDriverURI, debug);
                if (Driver == null)
                {
                    throw new InvalidOperationException("WinApp session is not reattched.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nReattachSession failed with winAppDriverExePath: {winAppDriverExePath}, winAppDriverURI: {winAppDriverURI}, applicationName: {applicationName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the XML source of the current application window's UI hierarchy.
        /// </summary>
        /// <returns>XML string representation of the complete application UI tree structure</returns>
        public string GetPageSource()
        {
            try
            {
                if (Driver == null)
                {
                    throw new InvalidOperationException("WinApp driver is not initialized. Call InitializeSession first.");
                }
                return Driver.PageSource;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetPageSource failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears existing text and enters new text into a UI element.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, etc.)</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetText(By locator, string text)
        {
            try
            {
                var element = GetElement(locator);
                element.Clear();
                element.SendKeys(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetText failed for locator: {locator} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears existing text and enters new text into a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to input text into</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetText(WindowsElement element, string text)
        {
            try
            {
                element.Clear();
                element.SendKeys(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetText failed for element and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the visible text content from a UI element.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, etc.)</param>
        /// <returns>Element's visible text content as string</returns>
        public string GetText(By locator)
        {
            try
            {
                var text = GetElement(locator).Text;
                return text;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetText failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the visible text content from a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to read text from</param>
        /// <returns>Element's visible text content as string</returns>
        public string GetText(WindowsElement element)
        {
            try
            {
                var text = element.Text;
                return text;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetText failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears all text content from an input element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void ClearText(By locator)
        {
            try
            {
                GetElement(locator).Clear();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClearText failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears all text content from a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to clear</param>
        public void ClearText(WindowsElement element)
        {
            try
            {
                element.Clear();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClearText failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing text and pressing Enter.
        /// </summary>
        /// <param name="locator">Dropdown element locator (By.Name, By.Id, etc.)</param>
        /// <param name="text">Option text to select</param>
        public void SelectDropdownByText(By locator, string text)
        {
            try
            {
                var element = GetElement(locator);
                element.Click();
                element.SendKeys(text + Keys.Enter);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByText failed for locator: {locator} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing text and pressing Enter.
        /// </summary>
        /// <param name="element">WindowsElement dropdown</param>
        /// <param name="text">Option text to select</param>
        public void SelectDropdownByText(WindowsElement element, string text)
        {
            try
            {
                element.Click();
                element.SendKeys(text + Keys.Enter);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByText failed for element and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by navigating to the specified index using arrow keys.
        /// </summary>
        /// <param name="locator">Dropdown element locator</param>
        /// <param name="index">Zero-based option index</param>
        public void SelectDropdownByIndex(By locator, int index)
        {
            try
            {
                var element = GetElement(locator);
                element.Click();
                for (int i = 0; i < index; i++)
                    element.SendKeys(Keys.ArrowDown);
                element.SendKeys(Keys.Enter);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByIndex failed for locator: {locator} and index: {index}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by navigating to the specified index using arrow keys.
        /// </summary>
        /// <param name="element">WindowsElement dropdown</param>
        /// <param name="index">Zero-based option index</param>
        public void SelectDropdownByIndex(WindowsElement element, int index)
        {
            try
            {
                element.Click();
                for (int i = 0; i < index; i++)
                    element.SendKeys(Keys.ArrowDown);
                element.SendKeys(Keys.Enter);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByIndex failed for element and index: {index}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing characters until the desired value is matched.
        /// </summary>
        /// <param name="locator">Dropdown element locator</param>
        /// <param name="text">Option text to match</param>
        /// <param name="codeToMatch">Specific code to match (optional, defaults to text parameter)</param>
        /// <param name="keyToUse">Key to use for sending input (optional, defaults to first character of text)</param>
        public void SelectDropdownByValue(By locator, string text, string codeToMatch = "", string keyToUse = "")
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
                    {
                        if (string.IsNullOrEmpty(keyToUse))
                            GetElement(locator).SendKeys(text[0].ToString());
                        else
                        {
                            Click(locator);
                            SendKeysIn(keyToUse);
                        }
                    }
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
        /// <param name="element">WindowsElement dropdown</param>
        /// <param name="text">Option text to match</param>
        /// <param name="codeToMatch">Specific code to match (optional, defaults to text parameter)</param>
        /// <param name="keyToUse">Key to use for sending input (optional, defaults to first character of text)</param>
        public void SelectDropdownByValue(WindowsElement element, string text, string codeToMatch = "", string keyToUse = "")
        {
            if (string.IsNullOrEmpty(codeToMatch))
                codeToMatch = text;
            try
            {
                while (true)
                {
                    if (GetText(element).Equals(codeToMatch))
                        break;
                    else
                    {
                        if (string.IsNullOrEmpty(keyToUse))
                            element.SendKeys(text[0].ToString());
                        else
                        {
                            element.Click();
                            SendKeysIn(keyToUse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for element and value: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing characters into a combobox until the display text matches the expected value.
        /// </summary>
        /// <param name="locatorForText">Locator of the element that displays the currently selected dropdown text/value.</param>
        /// <param name="locatorForCombobox">Locator of the combobox element that receives keyboard input.</param>
        /// <param name="text">Text used to navigate/select the dropdown option (first character is repeatedly sent).</param>
        /// <param name="codeToMatch">Expected text/value to stop on (optional, defaults to <paramref name="text"/>).</param>
        /// <param name="keyToUse">Key to use for sending input (optional, defaults to first character of <paramref name="text"/>).</param>
        public void SelectDropdownByValue(By locatorForCombobox, By locatorForText, string text, string codeToMatch = "", string keyToUse = "")
        {
            if (string.IsNullOrEmpty(codeToMatch))
                codeToMatch = text;
            try
            {
                while (true)
                {
                    if (GetText(locatorForText).Equals(codeToMatch))
                        break;
                    else
                    {
                        if (string.IsNullOrEmpty(keyToUse))
                            GetElement(locatorForCombobox).SendKeys(text[0].ToString());
                        else
                        {
                            Click(locatorForCombobox);
                            SendKeysIn(keyToUse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for TextLocator: {locatorForText} or ComboLocator: {locatorForCombobox}  and value: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Selects a dropdown option by typing characters into a combobox until the display text matches the expected value.
        /// </summary>
        /// <param name="locatorForText">Locator of the element that displays the currently selected dropdown text/value.</param>
        /// <param name="locatorForCombobox">Locator of the combobox element that receives keyboard input.</param>
        /// <param name="text">Text used to navigate/select the dropdown option (first character is repeatedly sent).</param>
        /// <param name="codeToMatch">Expected text/value to stop on (optional, defaults to <paramref name="text"/>).</param>
        /// <param name="keyToUse">Key to use for sending input (optional, defaults to first character of <paramref name="text"/>).</param>
        public void SelectDropdownByValue(WindowsElement locatorForCombobox, WindowsElement locatorForText, string text, string codeToMatch = "", string keyToUse = "")
        {
            if (string.IsNullOrEmpty(codeToMatch))
                codeToMatch = text;

            try
            {
                while (true)
                {
                    if (locatorForText.Text.Equals(codeToMatch))
                        break;
                    else
                        if (string.IsNullOrEmpty(keyToUse))
                            locatorForCombobox.SendKeys(text[0].ToString());
                        else
                        {
                            locatorForCombobox.Click();
                            SendKeysIn(keyToUse);
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSelectDropdownByValue failed for TextLocator: {locatorForText} or ComboLocator: {locatorForCombobox}  and value: {text}\n{ex.StackTrace}", ex);
            }
        }


        /// <summary>
        /// Waits for a UI element to become visible within the specified timeout.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds</param>
        /// <param name="elementName">Name of element for error reporting</param>
        /// <param name="foundelement">Output parameter containing the found element</param>
        /// <returns>True if element becomes visible, throws exception if timeout</returns>       
        public bool WaitForElementVisible(By locator, int timeoutInSeconds, string? elementName, out WindowsElement foundElement)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                while (watch.Elapsed.TotalSeconds < timeoutInSeconds)
                {
                    try
                    {
                        var element = GetElement(locator);
                        if (element.Displayed)
                        {
                            foundElement = element;
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore exceptions while waiting
                    }
                    //Utilities.WinApp.WaitForAppIdle(200); // Wait 200ms second before retrying
                }
                throw new Exception($"Couldn’t find {elementName} on the screen within {timeoutInSeconds} seconds. Unable to proceed further");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementVisible failed for locator: {locator} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        public bool WaitForDirectElementVisible(By locator, int timeoutInSeconds, string? elementName, out WindowsElement foundElement)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                while (watch.Elapsed.TotalSeconds < timeoutInSeconds)
                {
                    try
                    {
                        var element = GetElementDirect(locator);
                        if (element.Displayed)
                        {
                            foundElement = element;
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore exceptions while waiting
                    }
                    //Utilities.WinApp.WaitForAppIdle(200); // Wait 200ms second before retrying
                }
                throw new Exception($"Couldn’t find {elementName} on the screen within {timeoutInSeconds} seconds. Unable to proceed further");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementVisible failed for locator: {locator} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for a UI element to become clickable (visible and enabled).
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 10)</param>
        /// <returns>Clickable WindowsElement</returns>
        public WindowsElement WaitForElementClickable(By locator, int timeoutInSeconds = 10)
        {
            try
            {
                var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
                var el = wait.Until(drv =>
                {
                    var element = GetElement(locator);
                    return (element != null && element.Displayed && element.Enabled) ? element : null;
                });

                if (el == null)
                    throw new InvalidOperationException("Element is null after waiting.");
                return el;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementClickable failed for locator: {locator} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for a UI element to exist in the application's UI hierarchy.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="timeoutInSeconds">Timeout in seconds (default: 10)</param>
        /// <returns>Existing WindowsElement</returns>
        public WindowsElement WaitForElementExists(By locator, int timeoutInSeconds = 10)
        {
            try
            {
                var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
                var el = wait.Until(drv => GetElement(locator));

                if (el == null)
                    throw new InvalidOperationException("Element is null after waiting.");
                return el;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForElementExists failed for locator: {locator} with timeout: {timeoutInSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if an element is present and visible within the specified timeout.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="timeoutInSeconds">Maximum wait time in seconds</param>
        /// <param name="foundelement">Output parameter containing the found element (null if not found)</param>
        /// <returns>True if element is present and visible, false otherwise</returns>       
        public bool IsElementPresent(By locator, out WindowsElement? foundelement)
        {
            try
            {
                var element = GetElement(locator);
                foundelement = element;
                return element.Displayed;
            }
            catch (Exception)
            {
                foundelement = null;
                return false;
            }
        }

        /// <summary>
        /// Checks if a WindowsElement exists without throwing exceptions.
        /// </summary>
        /// <param name="element">WindowsElement to check</param>
        /// <returns>True if element is not null, false otherwise</returns>
        public bool IsElementPresent(WindowsElement element)
        {
            try { var present = element != null; return present; } catch { return false; }
        }

        /// <summary>
        /// Checks if a UI element is enabled for user interaction.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <returns>True if element is enabled, false otherwise</returns>
        public bool IsElementEnabled(By locator)
        {
            try { var enabled = GetElement(locator).Enabled; return enabled; } catch { return false; }
        }

        /// <summary>
        /// Checks if a WindowsElement is enabled for user interaction.
        /// </summary>
        /// <param name="element">WindowsElement to check</param>
        /// <returns>True if element is enabled, false otherwise</returns>
        public bool IsElementEnabled(WindowsElement element)
        {
            try { var enabled = element.Enabled; return enabled; } catch { return false; }
        }

        /// <summary>
        /// Checks if a UI element is currently visible on screen.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <returns>True if element is displayed, false otherwise</returns>
        public bool IsElementDisplayed(By locator)
        {
            try { var displayed = GetElement(locator).Displayed; return displayed; } catch { return false; }
        }

        /// <summary>
        /// Checks if a WindowsElement is currently visible on screen.
        /// </summary>
        /// <param name="element">WindowsElement to check</param>
        /// <returns>True if element is displayed, false otherwise</returns>
        public bool IsElementDisplayed(WindowsElement element)
        {
            try { var displayed = element.Displayed; return displayed; } catch { return false; }
        }

        /// <summary>
        /// Sends keyboard input to a UI element without clearing existing content.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, etc.)</param>
        /// <param name="keys">Keys or text to send (supports OpenQA.Selenium.Keys constants)</param>
        public void SendKeys(By locator, string keys)
        {
            try
            {
                GetElement(locator).SendKeys(keys);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSendKeys failed for locator: {locator} and keys: {keys}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sends keyboard input to a WindowsElement without clearing existing content.
        /// </summary>
        /// <param name="element">WindowsElement to send keys to</param>
        /// <param name="keys">Keys or text to send</param>
        public void SendKeys(WindowsElement element, string keys)
        {
            try
            {
                element.SendKeys(keys);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSendKeys failed for element and keys: {keys}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the value of a specified attribute from a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="attributeName">Name of the attribute to retrieve</param>
        /// <returns>Attribute value or null if not found</returns>
        public string GetAttribute(By locator, string attributeName)
        {
            try
            {
                var attr = GetElement(locator).GetAttribute(attributeName);
                return attr;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAttribute failed for locator: {locator} and attribute: {attributeName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the value of a specified attribute from a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to get attribute from</param>
        /// <param name="attributeName">Name of the attribute to retrieve</param>
        /// <returns>Attribute value or null if not found</returns>
        public string GetAttribute(WindowsElement element, string attributeName)
        {
            try
            {
                var attr = element.GetAttribute(attributeName);
                return attr;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAttribute failed for element and attribute: {attributeName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Counts the number of UI elements matching the specified locator.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <returns>Number of matching elements</returns>
        public int GetElementCount(By locator)
        {
            try
            {
                var count = GetElements(locator).Count;
                return count;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementCount failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to a different application window.
        /// </summary>
        /// <param name="windowName">Window handle or name to switch to</param>
        public void SwitchToWindow(string windowName)
        {
            try
            {
                Driver?.SwitchTo().Window(windowName);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchToWindow failed for windowName: {windowName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to a frame or embedded control within the application.
        /// </summary>
        /// <param name="locator">Frame element locator</param>
        public void SwitchToFrame(By locator)
        {
            try
            {
                var frame = GetElement(locator);
                Driver?.SwitchTo().Frame(frame);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchToFrame failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Switches focus to a frame or embedded control using a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement frame to switch to</param>
        public void SwitchToFrame(WindowsElement element)
        {
            try
            {
                if (Driver == null)
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");

                if (element == null)
                    throw new ArgumentNullException(nameof(element), "The provided WindowsElement is null.");

                Driver.SwitchTo().Frame(element);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSwitchToFrame failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Scrolls the application window to bring a UI element into view.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void ScrollToElement(By locator)
        {
            try
            {
                var element = GetElement(locator);
                element.SendKeys(OpenQA.Selenium.Keys.PageDown);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nScrollToElement failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Scrolls the application window to bring a WindowsElement into view.
        /// </summary>
        /// <param name="element">WindowsElement to scroll to</param>
        public void ScrollToElement(WindowsElement element)
        {
            try
            {
                element.SendKeys(OpenQA.Selenium.Keys.PageDown);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nScrollToElement failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets all available window handles for the current application session.
        /// </summary>
        /// <returns>Collection of window handles</returns>
        public ReadOnlyCollection<string> GetWindowHandles()
        {
            try
            {
                if (Driver == null)
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");

                var handles = Driver.WindowHandles;
                return handles ?? new ReadOnlyCollection<string>(new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetWindowHandles failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Pauses test execution for the specified duration to allow application processing.
        /// </summary>
        /// <param name="timeoutInMilliSeconds">Wait time in milliseconds (default: 1000)</param>
        public void WaitForAppIdle(int timeoutInMilliSeconds = 1000)
        {
            try
            {
                System.Threading.Thread.Sleep(timeoutInMilliSeconds);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForAppIdle failed with timeout: {timeoutInMilliSeconds} seconds\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for a UI element's text content to change from its current value.
        /// </summary>
        /// <param name = "locator" > Element locator</param>      
        /// <param name = "timeoutInSeconds" > Timeout in seconds(default: 5)</param>
        /// <param name="foundelement">Output parameter containing the found element</param>
        public string WaitForTextChange(By locator, int timeoutInSeconds, out WindowsElement foundElement)
        {
            try
            {
                var element = GetElement(locator);
                string initialText = element.Text;
                var watch = Stopwatch.StartNew();

                while (watch.Elapsed.TotalSeconds < timeoutInSeconds)
                {
                    var currentText = element.Text;
                    if (!initialText.Equals(currentText, StringComparison.OrdinalIgnoreCase))
                    {
                        foundElement = element;
                        return currentText;
                    }
                }
                foundElement = element;
                return initialText;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitForTextChange failed for locator: {locator} with timeout: {timeoutInSeconds} seconds.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the screen coordinates (X, Y position) of a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <returns>Point containing the element's screen coordinates</returns>
        public Point GetElementLocation(By locator)
        {
            try
            {
                var location = GetElement(locator).Location;
                return location;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementLocation failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the screen coordinates (X, Y position) of a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to get location from</param>
        /// <returns>Point containing the element's screen coordinates</returns>
        public Point GetElementLocation(WindowsElement element)
        {
            try
            {
                var location = element.Location;
                return location;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementLocation failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the dimensions (width and height) of a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <returns>Size containing the element's width and height</returns>
        public Size GetElementSize(By locator)
        {
            try
            {
                var size = GetElement(locator).Size;
                return size;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementSize failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the dimensions (width and height) of a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to get size from</param>
        /// <returns>Size containing the element's width and height</returns>
        public Size GetElementSize(WindowsElement element)
        {
            try
            {
                var size = element.Size;
                return size;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementSize failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds all child elements of a parent element matching the specified locators.
        /// </summary>
        /// <param name="locator_Parent">Parent element locator</param>
        /// <param name="locator_Child">Child elements locator</param>
        /// <returns>List of matching child AppiumWebElements</returns>
        public List<AppiumWebElement> GetChildren(By locator_Parent, By locator_Child)
        {
            try
            {
                List<AppiumWebElement> els = GetElement(locator_Parent).FindElements(locator_Child).ToList();
                return els;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetChildren failed for parent locator: {locator_Parent} and child locator: {locator_Child}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds a single child element within a specified parent WindowsElement.
        /// </summary>
        /// <param name="element_Parent">Parent WindowsElement</param>
        /// <param name="locator_Child">Child element locator</param>
        /// <returns>Single matching child WindowsElement</returns>
        public WindowsElement GetUniqueChildren(WindowsElement element_Parent, By locator_Child)
        {
            try
            {
                WindowsElement el = (WindowsElement)element_Parent.FindElement(locator_Child);
                return el;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetUniqueChildren failed for parent element and child locator: {locator_Child}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds all child elements of a parent AppiumWebElement matching the specified locator.
        /// </summary>
        /// <param name="element_Parent">Parent AppiumWebElement</param>
        /// <param name="locator_Child">Child elements locator</param>
        /// <returns>List of matching child AppiumWebElements</returns>
        public List<AppiumWebElement> GetChildren(AppiumWebElement element_Parent, By locator_Child)
        {
            try
            {
                List<AppiumWebElement> els = element_Parent.FindElements(locator_Child).ToList();
                return els;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetChildren failed for parent element and child locator: {locator_Child}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds a single child element within a parent element using locators.
        /// </summary>
        /// <param name="locator_Parent">Parent element locator</param>
        /// <param name="locator_Child">Child element locator</param>
        /// <returns>Single matching child WindowsElement</returns>
        public WindowsElement GetUniqueChildren(By locator_Parent, By locator_Child)
        {
            try
            {
                WindowsElement el = (WindowsElement)GetElement(locator_Parent).FindElement(locator_Child);
                return el;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetUniqueChildren failed for parent locator: {locator_Parent} and child locator: {locator_Child}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds a UI element using the specified locator with retry mechanism.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, By.ClassName, etc.)</param>        
        /// <returns>WindowsElement object for further interaction</returns>
        public WindowsElement GetElement(By locator)
        {
            try
            {
                if (locator == null)
                    throw new ArgumentNullException(nameof(locator), "Locator cannot be null.");

                if (Driver == null)
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");

                if (locator.ToString().Contains("By.XPath:"))
                {
                    return GetElementByXpath(locator);
                }
                else
                {
                    return GetElementDirect(locator);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElement failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Directly finds a UI element using the driver without XPath processing.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, By.ClassName, etc.)</param>        
        /// <returns>WindowsElement object for further interaction</returns>
        public WindowsElement GetElementDirect(By locator)
        {
            try
            {
                if (locator == null)
                    throw new ArgumentNullException(nameof(locator), "Locator cannot be null.");

                if (Driver == null)
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");

                for (int i = 0; i < 3; i++) // Retry up to 3 times
                {
                    try
                    {
                        WindowsElement el = (WindowsElement)Driver.FindElement(locator);
                        return el;
                    }
                    catch (Exception)
                    {
                        // Continue to retry
                    }
                }
                throw new ElementNotVisibleException();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementDirect failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Directly finds multiple UI elements using the driver without XPath processing.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, By.ClassName, etc.)</param>
        /// <returns>List of matching WindowsElements for bulk operations</returns>
        private List<WindowsElement> GetElementsDirect(By locator)
        {
            try
            {
                if (locator == null)
                    throw new ArgumentNullException(nameof(locator), "Locator cannot be null.");

                if (Driver == null)
                    throw new InvalidOperationException("WinApp session is not initialized. Call InitializeSession and ensure WinAppDriver is running and the application is available.");

                for (int i = 0; i < 3; i++) // Retry up to 3 times
                {
                    try
                    {
                        return Driver.FindElements(locator)?.Select(e => (WindowsElement)e).ToList() ?? new List<WindowsElement>();
                    }
                    catch (Exception)
                    {
                        // Continue to retry
                    }
                }
                throw new ElementNotVisibleException();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementsDirect failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds all UI elements matching the specified locator with retry mechanism.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, By.ClassName, etc.)</param>
        /// <returns>List of matching WindowsElements for bulk operations</returns>
        public List<WindowsElement> GetElements(By locator)
        {
            return GetElementsDirect(locator);
        }

        /// <summary>
        /// Splits an XPath string into segments while respecting bracket depth.
        /// </summary>
        /// <param name="xpath">XPath string to split into segments</param>
        /// <returns>List of XPath segments for sequential element traversal</returns>
        private List<string> SplitXPathSafely(string xpath)
        {
            var segments = new List<string>();
            var sb = new StringBuilder();
            int bracketDepth = 0;

            for (int i = 0; i < xpath.Length; i++)
            {
                char c = xpath[i];

                if (c == '/' && bracketDepth == 0)
                {
                    if (i + 1 < xpath.Length && xpath[i + 1] == '/')
                    {
                        // Skip extra slash in //
                        i++;
                    }

                    if (sb.Length > 0)
                    {
                        segments.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    if (c == '[') bracketDepth++;
                    if (c == ']') bracketDepth--;
                    sb.Append(c);
                }
            }

            if (sb.Length > 0)
                segments.Add(sb.ToString());

            return segments;
        }

        /// <summary>
        /// Finds a UI element using XPath by converting it to sequential By locators.
        /// </summary>
        /// <param name="fullXPath">Complete XPath expression to locate the target element</param>
        /// <returns>WindowsElement matching the XPath expression</returns>
        /// <exception cref="ArgumentException">Thrown when XPath segment format is invalid</exception>
        private WindowsElement GetElementByXpath(By fullXPath)
        {
            try
            {
                if (fullXPath == null)
                    throw new ArgumentNullException(nameof(fullXPath), "XPath cannot be null.");

                var segments = SplitXPathSafely(fullXPath.ToString().Replace("By.XPath: ", "").Trim('/'));
                WindowsElement? currentElement = null;

                foreach (var segment in segments)
                {
                    var match = Regex.Match(segment, @"^(?<tag>\w+)(\[@(?<attr>\w+)='(?<value>[^']+)'\])?");
                    if (!match.Success)
                        throw new ArgumentException($"Invalid segment format: {segment}");

                    string tag = match.Groups["tag"].Value;
                    string attr = match.Groups["attr"].Value;
                    string value = match.Groups["value"].Value;
                    By? by = null;

                    if (!string.IsNullOrEmpty(attr))
                    {
                        switch (attr.ToLower())
                        {
                            case "name":
                                by = By.Name(value);
                                break;
                            case "automationid":
                                by = MobileBy.AccessibilityId(value);
                                break;
                            case "classname":
                                by = By.ClassName(value);
                                break;
                        }
                    }
                    else
                        by = By.XPath($"//{tag}");

                    if (by == null)
                        throw new ArgumentNullException(nameof(by), "Generated locator cannot be null.");

                    // Use GetElementDirect to avoid infinite recursion when by is an XPath
                    currentElement = (WindowsElement?)(currentElement == null ? GetElementDirect(by) : currentElement.FindElement(by));
                }

                if (currentElement == null)
                    throw new InvalidOperationException("Element not found for the provided XPath.");

                return currentElement;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementByXpath failed for XPath: {fullXPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds multiple UI elements using XPath by converting it to sequential By locators.
        /// </summary>
        /// <param name="fullXPath">Complete XPath expression to locate the target elements</param>
        /// <returns>List of WindowsElements matching the XPath expression, or empty list if no matches found</returns>
        /// <exception cref="ArgumentException">Thrown when XPath segment format is invalid</exception>
        private List<WindowsElement> GetElementsListByXpath(By fullXPath)
        {
            try
            {
                var segments = SplitXPathSafely(fullXPath.ToString().Replace("By.XPath: ", "").Trim('/'));
                WindowsElement? currentElement = null;

                for (int i = 0; i < segments.Count; i++)
                {
                    var match = Regex.Match(segments[i], @"^(?<tag>\w+)(\[@(?<attr>\w+)='(?<value>[^']+)'\])?");
                    if (!match.Success)
                        throw new ArgumentException($"Invalid segment format: {segments[i]}");

                    string tag = match.Groups["tag"].Value;
                    string attr = match.Groups["attr"].Value;
                    string value = match.Groups["value"].Value;
                    By? by = null;

                    if (!string.IsNullOrEmpty(attr))
                    {
                        switch (attr.ToLower())
                        {
                            case "name":
                                by = By.Name(value);
                                break;
                            case "automationid":
                                by = MobileBy.AccessibilityId(value);
                                break;
                            case "classname":
                                by = By.ClassName(value);
                                break;
                        }
                    }
                    else
                        by = By.XPath($"//{tag}");

                    if (i == segments.Count - 1)
                    {
                        // Use GetElementsDirect to avoid infinite recursion when by is an XPath
                        return (currentElement == null
                            ? GetElementsDirect(by!).Select(e => (WindowsElement)e).ToList()
                            : currentElement.FindElements(by).Select(e => (WindowsElement)e).ToList());
                    }

                    if (by == null)
                        throw new ArgumentNullException(nameof(by), "Generated locator cannot be null.");

                    // Use GetElementDirect to avoid infinite recursion when by is an XPath
                    currentElement = (WindowsElement?)(currentElement == null ? GetElementDirect(by) : currentElement.FindElement(by));
                }

                // Return an empty list if no elements are found
                return new List<WindowsElement>();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetElementsListByXpath failed for XPath: {fullXPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a left-click action on a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void Click(By locator)
        {
            try
            {
                GetElement(locator).Click();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClick failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a left-click action on a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to click</param>
        public void Click(WindowsElement element)
        {
            try
            {
                element.Click();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nClick failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a double-click action on a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void DoubleClick(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.DoubleClick(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDoubleClick failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a double-click action on a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to double-click</param>
        public void DoubleClick(WindowsElement element)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.DoubleClick(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDoubleClick failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a right-click (context menu) action on a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void RightClick(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.ContextClick(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRightClick failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a right-click (context menu) action on a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to right-click</param>
        public void RightClick(WindowsElement element)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.ContextClick(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRightClick failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Hovers the mouse cursor over a UI element without clicking.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void HoverOverElement(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.MoveToElement(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nHoverOverElement failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Hovers the mouse cursor over a WindowsElement without clicking.
        /// </summary>
        /// <param name="element">WindowsElement to hover over</param>
        public void HoverOverElement(WindowsElement element)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.MoveToElement(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nHoverOverElement failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a drag and drop action between two UI elements.
        /// </summary>
        /// <param name="sourceLocator">Source element locator</param>
        /// <param name="targetLocator">Target element locator</param>
        public void DragAndDrop(By sourceLocator, By targetLocator)
        {
            try
            {
                var source = GetElement(sourceLocator);
                var target = GetElement(targetLocator);
                var actions = new Actions(Driver);
                actions.DragAndDrop(source, target).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDragAndDrop failed from {sourceLocator} to {targetLocator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Performs a drag and drop action between two WindowsElements.
        /// </summary>
        /// <param name="sourceElement">Source WindowsElement</param>
        /// <param name="targetElement">Target WindowsElement</param>
        public void DragAndDrop(WindowsElement sourceElement, WindowsElement targetElement)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.DragAndDrop(sourceElement, targetElement).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDragAndDrop failed from source element to target element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Moves the mouse cursor to hover over a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        public void MoveToElement(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.MoveToElement(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nMoveToElement failed for locator: {locator}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Moves the mouse cursor to hover over a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement to move to</param>
        public void MoveToElement(WindowsElement element)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.MoveToElement(element).Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nMoveToElement failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks at specific coordinates relative to a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="x">X coordinate offset from element</param>
        /// <param name="y">Y coordinate offset from element</param>
        public void CordinateClick(By locator, int x, int y)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.MoveToElement(element, x, y).Click().Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCordinateClick failed for locator\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks at specific coordinates relative to a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement reference point</param>
        /// <param name="x">X coordinate offset from element</param>
        /// <param name="y">Y coordinate offset from element</param>
        public void CordinateClick(WindowsElement element, int x, int y)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.MoveToElement(element, x, y).Click().Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCordinateClick failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks at specific coordinates relative to a UI element.
        /// </summary>
        /// <param name="locator">Element locator</param>
        /// <param name="x">X coordinate offset from element</param>
        /// <param name="y">Y coordinate offset from element</param>
        public void CordinateRightClick(By locator, int x, int y)
        {
            try
            {
                var element = GetElement(locator);
                var actions = new Actions(Driver);
                actions.MoveToElement(element, x, y).ContextClick().Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCordinateRightClick failed for locator\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clicks at specific coordinates relative to a WindowsElement.
        /// </summary>
        /// <param name="element">WindowsElement reference point</param>
        /// <param name="x">X coordinate offset from element</param>
        /// <param name="y">Y coordinate offset from element</param>
        public void CordinateRightClick(WindowsElement element, int x, int y)
        {
            try
            {
                var actions = new Actions(Driver);
                actions.MoveToElement(element, x, y).ContextClick().Perform();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCordinateRightClick failed for element\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears text and enters new text using native Win32 API input automation.
        /// </summary>
        /// <param name="locator">Element locator (By.Name, By.Id, etc.)</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetTextIn(By locator, string text)
        {
            try
            {
                var element = GetElement(locator);
                ClearText(element);
                Click(element);
                Utilities.Input.InputText(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetTextIn failed for locator: {locator} and text: {text}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Clears text and enters new text using native Win32 API input automation.
        /// </summary>
        /// <param name="element">WindowsElement to input text into</param>
        /// <param name="text">Text to enter into the element</param>
        public void SetTextIn(WindowsElement element, string text)
        {
            try
            {
                ClearText(element);
                Click(element);
                Utilities.Input.InputText(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetText failed for element and text: {text}\n{ex.StackTrace}", ex);
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
                case "\uE017": // Delete
                    Utilities.Input.InputDelete();
                    break;
            }
        }
    }
}

