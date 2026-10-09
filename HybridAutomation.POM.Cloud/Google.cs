using HybridAutomation.Helpers;

namespace HybridAutomation.POM.Cloud
{
    public class Google
    {
        private string SerachBoxSelector = "#APjFqb";

        public void Search(string searchQuery)
        {
            Utilities.Playwright.SetText(SerachBoxSelector, searchQuery);
            Utilities.Input.InputEnter();
        }
    }
}
