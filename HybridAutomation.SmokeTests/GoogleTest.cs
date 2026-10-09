using HybridAutomation.Helpers;
using HybridAutomation.POM.Cloud;

namespace HybridAutomation.SmokeTests
{
    public class GoogleTest : TestBase
    {
        [Test]
        public void GoogleSearchTest()
        {
            Utilities.Playwright.InitializeDriver(Browser?? "chromium", headless: false, downloadPath : TestArtefacts);
            Utilities.Playwright.NavigateToURL("https://www.google.com");
            Cloud.Google.Search("Playwright C#");            
        }
    }
}
