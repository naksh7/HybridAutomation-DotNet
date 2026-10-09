using HybridAutomation.Helpers;

namespace HybridAutomation.SmokeTests
{
    public class AppSettings
    {
        public string? WinAppDriverURI { get; set; } = string.Empty;
        public string? WinAppDriverExePath { get; set; } = string.Empty;
        public string? ApplicationName { get; set; } = string.Empty;
        public string? Browser { get; set; } = string.Empty;
        public string? AzureUrl { get; set; } = string.Empty;
        public string? AzureOrganization { get; set; } = string.Empty;
        public string? AzureProject { get; set; } = string.Empty;
        public string? VSTSPAT { get; set; } = string.Empty;
        public string? TestArtefacts { get; set; } = string.Empty;
    }
     
    public abstract class TestBase
    {
        
        public static AppSettings? Parameters;   
        public static string? TestArtefacts;    
        public static string? Browser;
        public static string? HTMLReport;        
        public static string? TraceReport;        
        public static DateTime? TimeStamp;

        [OneTimeSetUp]
        public void Initialize()
        {
            TimeStamp = DateTime.Now;
            string configPath = Path.Combine(Environment.CurrentDirectory ?? string.Empty, "appsettings.json");
            Parameters = Utilities.FileReader.ReadConfig<AppSettings>(configPath);
                        
            if (Parameters == null ||                
                string.IsNullOrEmpty(Parameters.AzureUrl) ||
                string.IsNullOrEmpty(Parameters.AzureOrganization) ||
                string.IsNullOrEmpty(Parameters.AzureProject) ||
                string.IsNullOrEmpty(Parameters.VSTSPAT) ||                
                string.IsNullOrEmpty(Parameters.TestArtefacts) ||
                string.IsNullOrEmpty(Parameters.WinAppDriverExePath) ||
                string.IsNullOrEmpty(Parameters.WinAppDriverURI) ||
                string.IsNullOrEmpty(Parameters.ApplicationName) ||
                string.IsNullOrEmpty(Parameters.Browser))
            {
                throw new InvalidOperationException("Configuration parameters are missing in appsettings.json.");
            }            
            
            TestArtefacts = Path.Combine(Parameters.TestArtefacts, TestContext.CurrentContext.Test.Name+"_"+ TimeStamp?.ToString("dd.MMM.yyyy_HH.mm.ss"));
            Browser = Parameters.Browser;
            // Generate timestamped file paths for HTML and trace reports
            HTMLReport = Path.Combine(TestArtefacts, $"ExtentReport_{TimeStamp?.ToString("dd.MMM.yyyy_HH.mm.ss")}.html");           
            TraceReport = Path.Combine(TestArtefacts, $"TracetReport_{TimeStamp?.ToString("dd.MMM.yyyy_HH.mm.ss")}.txt");
                
            Utilities.Logger.Initialize(TraceReport, HTMLReport, TestContext.CurrentContext.Test.Name);
            Utilities.Azure.Initialize(Parameters.AzureUrl, Parameters.AzureOrganization, Parameters.AzureProject, Parameters.VSTSPAT);            
        }
       
        [SetUp]
        public void StartLogging()
        {
            if (string.IsNullOrEmpty(TestArtefacts))            
                throw new InvalidOperationException("TestArtefacts is null or empty. Ensure it is initialized before calling StartLogging.");            
            Utilities.Logger.StartLogging(TestContext.CurrentContext.Test.Name, TestArtefacts);           
        }
              
        [TearDown]
        public void TearDown()
        {            
            Utilities.Logger.Flush(TestContext.CurrentContext.Test.Name);            
            if (!string.IsNullOrEmpty(HTMLReport))           
                TestContext.AddTestAttachment(HTMLReport);            
        }
               
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Utilities.ProcessHandler.EndProcess("WinAppDriver");          
            if (Parameters != null && !string.IsNullOrEmpty(Parameters.ApplicationName))            
                Utilities.ProcessHandler.EndProcess(Parameters.ApplicationName); 
            Utilities.Logger.OneTimeFlush(TestContext.CurrentContext.Test.Name);   
            Utilities.DriverManager.CloseAll();
        }
    }
}
