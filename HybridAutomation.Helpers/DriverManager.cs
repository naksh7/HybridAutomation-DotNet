using Microsoft.Playwright;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace HybridAutomation.Helpers
{
    public class PlaywrightDetails
    {
        public IPlaywright? PlaywrightInstance { get; set; }
        public IBrowser? BrowserInstance { get; set; }
        public IBrowserContext? BrowserContextInstance { get; set; }
        public IPage? PageInstance { get; set; }
    }

    public class DriverManager
    {
        public WindowsDriver<WindowsElement>? Session = null;
        public Playwright? Playwright = null;
        public PlaywrightDetails? PlaywrightDetails = null;

        /// <summary>
        /// Creates and configures a Playwright instance with DriverManager integration for centralized session management.
        /// Enhanced with unified capabilities and automatic browser name mapping.
        /// </summary>
        /// <param name="browser">Browser type: "chromium", "firefox", "webkit", "chrome", "safari" (automatic mapping applied)</param>
        /// <param name="downloadPath">Download directory path (optional)</param>
        /// <param name="headless">Run browser in headless mode (default: false)</param>
        /// <param name="slowMo">Slow motion delay in milliseconds for debugging (default: 0)</param>
        /// <returns>Configured Playwright instance with DriverManager integration</returns>
        public PlaywrightDetails CreatePlaywrightDriver(string browserType = "chromium", bool headless = false, string? downloadPath = null)
        {
            try
            {
                PlaywrightDetails = new PlaywrightDetails();
                // Use the existing implementation to maintain full backward compatibility
                PlaywrightDetails.PlaywrightInstance = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();

                PlaywrightDetails.BrowserInstance = browserType.ToLower() switch
                {
                    "firefox" => PlaywrightDetails.PlaywrightInstance.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless }).GetAwaiter().GetResult(),
                    "webkit" => PlaywrightDetails.PlaywrightInstance.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless }).GetAwaiter().GetResult(),
                    "edge" => PlaywrightDetails.PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Channel = "msedge", Args = ["--start-maximized"] }).GetAwaiter().GetResult(),
                    "chromium" => PlaywrightDetails.PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Args = ["--start-maximized"] }).GetAwaiter().GetResult(),
                    _ => PlaywrightDetails.PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Args = ["--start-maximized"] }).GetAwaiter().GetResult()
                };

                var contextOptions = new BrowserNewContextOptions { ViewportSize = ViewportSize.NoViewport };
                if (!string.IsNullOrEmpty(downloadPath))
                {
                    contextOptions.AcceptDownloads = true;
                }

                PlaywrightDetails.BrowserContextInstance = PlaywrightDetails.BrowserInstance.NewContextAsync(contextOptions).GetAwaiter().GetResult();
                PlaywrightDetails.PageInstance = PlaywrightDetails.BrowserContextInstance.NewPageAsync().GetAwaiter().GetResult();

                return PlaywrightDetails;
            }
            catch (PlaywrightException ex)
            {
                try
                {
                    // Automatically install Playwright browsers when they are missing
                    string playwrightScript = System.IO.Path.Combine(AppContext.BaseDirectory, "playwright.ps1");
                    var result = Utilities.ProcessHandler.ExecuteProcess("powershell.exe", $"-ExecutionPolicy Bypass -File \"{playwrightScript}\" install");

                    if (!result.IsExecuted || result.ExitCode != 0)
                        throw new Exception($"Playwright browser installation failed.\nOutput: {result.OutputMessage}\nError: {result.ErrorMessage}");

                    // Retry after installation
                    return CreatePlaywrightDriver(browserType, headless, downloadPath);
                }
                catch (Exception installEx)
                {
                    throw new Exception(
                        $"Playwright browsers are not installed and automatic installation failed.\n" +
                        $"Please run manually: pwsh bin/Debug/net*/playwright.ps1 install\n" +
                        $"Details: {installEx.Message}", installEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nInitializeDriver failed for browser: {browserType}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Creates and configures a WinAppDriver session for desktop application automation with process management.       
        /// </summary>
        /// <param name="applicationPath">Full path to the target application executable for launching</param>
        /// <param name="applicationName">Process name of the target application for session attachment</param>
        /// <param name="winAppDriverExePath">Full path to WinAppDriver.exe executable</param>
        /// <param name="winAppDriverURI">WinAppDriver service URI endpoint (e.g., http://127.0.0.1:4723)</param>
        /// <param name="debug">Debug mode flag - when true, skips application launch for debugging scenarios</param>
        /// <returns>Configured WindowsDriver instance with 500ms implicit timeout, or null if session creation fails</returns>
        public WindowsDriver<WindowsElement>? CreateSession(string applicationPath, string applicationName, string winAppDriverExePath, string winAppDriverURI, bool debug)
        {
            try
            {
                if (!debug)
                    Utilities.ProcessHandler.ExecuteProcess("cmd.exe", $"/c {applicationPath}");
                Utilities.ProcessHandler.EndProcess("WinAppDriver");
                if (Utilities.ProcessHandler.ExecuteProcess(winAppDriverExePath, redirectOutput: false).IsExecuted)
                {
                    AppiumOptions appCapabilities = new AppiumOptions();
                    Utilities.ProcessHandler.WaitTillProcessIsVisible(applicationName);
                    appCapabilities.AddAdditionalCapability("appTopLevelWindow", Utilities.ProcessHandler.GetExistingProcessHandle(applicationName));
                    appCapabilities.AddAdditionalCapability("deviceName", "WindowsPC");
                    appCapabilities.AddAdditionalCapability("ms:experimental-webdriver", true);
                    Session = new WindowsDriver<WindowsElement>(new Uri(winAppDriverURI), appCapabilities);
                    Utilities.Logger.Log(Logger.LogType.Info, $"Attached {applicationName} with {winAppDriverURI}");
                    Session.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(50);
                    Utilities.ProcessHandler.BringApplicationToFront(applicationName);
                    if (Session == null)
                        throw new Exception($"Failed to create session for {applicationName}. Please ensure WinAppDriver is running and the application is accessible.");

                    else
                        return Session;
                }
                else
                {
                    throw new Exception("WinAppDriver not installed in machine. Please download from https://github.com/microsoft/WinAppDriver and then install.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCreateSession failed for winAppDriverExePath: {winAppDriverExePath}, winAppDriverURI: {winAppDriverURI}, applicationName: {applicationName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Captures Base64-encoded screenshot from the currently active automation session (Selenium, Playwright, or WinAppDriver).
        /// </summary>
        /// <returns>Base64-encoded screenshot string for HTML report embedding, or empty string if no active session supports screenshots</returns>
        public string GetBase64Screenshot()
        {
            try
            {
                // Try Playwright
                if (PlaywrightDetails != null && PlaywrightDetails.PageInstance != null)
                {
                    return Convert.ToBase64String(PlaywrightDetails.PageInstance.ScreenshotAsync().GetAwaiter().GetResult());
                }

                // Try WinAppDriver last
                if (Session is ITakesScreenshot screenshotSession)
                {
                    return screenshotSession.GetScreenshot().AsBase64EncodedString;
                }

                return "";
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(Logger.LogType.Error, $"{ex.Message}\nGetBase64Screenshot failed.\n{ex.StackTrace}");
                return "";
            }
        }

        /// <summary>
        /// Closes all active automation sessions of browser and cleans up resources.
        /// </summary>
        public void CloseBrowser()
        {
            try
            {
                // Close Playwright
                if (Utilities.Playwright != null)
                {
                    Utilities.Playwright.CloseApp();
                }
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(Logger.LogType.Error, $"{ex.Message}\nCloseBrowser failed.\n{ex.StackTrace}");
            }
        }



        /// <summary>
        /// Closes all active automation sessions and cleans up resources.
        /// </summary>
        public void CloseAll()
        {
            try
            {

                // Close Playwright
                if (Utilities.Playwright != null)
                {
                    Utilities.Playwright.CloseApp();
                }

                // Close WinAppDriver session
                if (Session != null)
                {
                    Session.Quit();
                    Session.Dispose();
                    Session = null;
                }
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(Logger.LogType.Error, $"{ex.Message}\nCloseAll failed.\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets the active driver type for the current session.
        /// </summary>
        /// <returns>String indicating active driver type: "Selenium", "Playwright", "WinAppDriver", or "None"</returns>
        public string GetActiveDriverType()
        {
            if (Utilities.Playwright != null) return "Playwright";
            if (Session != null) return "WinAppDriver";
            return "None";
        }
    }
}
