using HybridAutomation.Helpers;

namespace HybridAutomation.SmokeTests
{
    public class Prerequisites : TestBase
    {       
        // Please don't remove this method. 
        [Test]
        public void EnvironmentSetup()
        {
            //Folder path can be changed as per the requirement
            string folderPath = @"C:\Hybrid Automation\Tools";
            Utilities.Files.EnsureDirectoryExist(folderPath);
            Utilities.EnvironmentSetup.EnableDeveloperMode();
            Utilities.EnvironmentSetup.SetUpWinAppDriver("https://github.com/microsoft/WinAppDriver/releases/download/v1.2.1/WindowsApplicationDriver_1.2.1.msi", folderPath);
            Utilities.EnvironmentSetup.SetupDiffPdfToolSystemWide("https://github.com/vslavik/diff-pdf/releases/download/v0.5.2/diff-pdf-win-0.5.2.zip", folderPath);
        }

        [Test]
        public void EssentialMethodShortcuts()
        {
            Utilities.WinApp.InitializeSession("", Parameters?.ApplicationName ?? string.Empty, Parameters?.WinAppDriverExePath ?? string.Empty, Parameters?.WinAppDriverURI ?? string.Empty, debug: true);
        }

        [Test]
        public void XpathFinder()
        {
            Utilities.WinApp.InitializeSession("", Parameters?.ApplicationName ?? string.Empty, Parameters?.WinAppDriverExePath ?? string.Empty, Parameters?.WinAppDriverURI ?? string.Empty, debug: true);
            List<string> xpathsToFind = new List<string>
            {
                "PageUp",
                "PageDown",
                "Row0_Amount",
                "Row0_AmountFY1",
                "Row0_DeductionFY1",
            };
            Utilities.Xpath.XpathsFinderAll(xpathsToFind, Utilities.WinApp.GetPageSource());
        }
    }
}
