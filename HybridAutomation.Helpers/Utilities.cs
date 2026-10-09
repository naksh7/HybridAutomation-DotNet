using System.Runtime.Versioning;

namespace HybridAutomation.Helpers
{
    public class Utilities
    {
        private static Logger? _logger;        
        private static ProcessHandler? _processHandler;
        private static Files? _files;
        private static WinApp? _winApp;
        private static FileReader? _fileReader;
        private static Excel? _excel;
        private static Xpath? _xpath;
        private static PDF? _pdf;
        private static EnvironmentSetup? _environmentSetup;
        private static SQL? _sQL;
        private static Input? _input;
        private static Speech? _speech;
        private static DriverManager? _driverManager;
        private static Azure? _azure;
        private static Msaa? _msaa;
        private static Playwright? _playwright;
        private static RestSharp? _restSharp;
        private static Regexp? _regexp;
        private static Json? _json;
        private static AcroLocators? _acroLocators;
        
        public static Json Json => _json ??= new Json();
        public static Regexp Regexp => _regexp ??= new Regexp();
        public static RestSharp RestSharp => _restSharp ??= new RestSharp();       
        public static Azure Azure => _azure ??= new Azure();
        public static DriverManager DriverManager => _driverManager ??= new DriverManager();
        public static Speech Speech => _speech ??= new Speech();
        public static Input Input => _input ??= new Input();
        public static SQL SQL => _sQL ??= new SQL();        
        public static WinApp WinApp => _winApp ??= new WinApp();
        public static Msaa Msaa => _msaa ??= new Msaa(); // Added
        public static Playwright Playwright => _playwright ??= new Playwright(); // Added Playwright
        [SupportedOSPlatform("windows")]
        public static AcroLocators AcroLocators => _acroLocators ??= new AcroLocators();
        public static Logger Logger => _logger ??= new Logger();
        public static ProcessHandler ProcessHandler => _processHandler ??= new ProcessHandler();
        public static Files Files => _files ??= new Files();
        public static FileReader FileReader => _fileReader ??= new FileReader();
        public static Excel Excel => _excel ??= new Excel();
        public static Xpath Xpath => _xpath ??= new Xpath();
        public static PDF PDF => _pdf ??= new PDF();
        public static EnvironmentSetup EnvironmentSetup => _environmentSetup ??= new EnvironmentSetup();

        /// <summary>
        /// Resets all utility instances. Useful for test cleanup or when fresh instances are needed.
        /// </summary>
        public static void ResetAll()
        {
            _regexp = null;
            _logger = null;
            _azure = null;            
            _processHandler = null;
            _files = null;
            _winApp = null;
            _fileReader = null;
            _excel = null;
            _xpath = null;
            _pdf = null;
            _environmentSetup = null;
            _sQL = null;
            _input = null;
            _speech = null;
            
            // Properly close all DriverManager sessions before resetting
            try { _driverManager?.CloseAll(); } catch { }
            _driverManager = null; 
            
            // Dispose Msaa to release any cached COM RCWs before clearing reference
            try { _msaa?.Dispose(); } catch { }
            _msaa = null;
            
            // Reset Playwright reference (DriverManager handles Playwright cleanup)
            _playwright = null;

            // Reset RestSharp client
            try { _restSharp?.Dispose(); } catch { }
            _restSharp = null;
        }
    }
}
