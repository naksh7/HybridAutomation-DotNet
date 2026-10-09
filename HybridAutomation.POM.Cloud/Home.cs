using HybridAutomation.Helpers;

namespace HybridAutomation.POM.Cloud
{
    public class Home
    {
        private string tileName = "";
        private string HomePage = "//div[text()='Home']";
        private string Tile => $"//md-tile[@id='{tileName}']";
              

        public void NavigateToTile(string tile)
        {
            tileName = tile;
            Utilities.Playwright.Click(Tile);            
        }
    }
}
