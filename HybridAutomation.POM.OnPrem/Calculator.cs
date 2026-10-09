using HybridAutomation.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace HybridAutomation.POM.OnPrem
{
    public class Calculator
    {
        private string ButtonName = string.Empty;
        private By Button => By.Name(ButtonName);
        private By Result => MobileBy.AccessibilityId("CalculatorResults");

        public void ClickButton(string buttonName)
        {
            ButtonName = buttonName;
            Utilities.WinApp.Click(Button);
            Utilities.Logger.Log(Logger.LogType.Info, "Clicked button: " + buttonName);
        }

        public string GetResultText()
        {
            return Utilities.WinApp.GetText(Result);            
        }
    }
}
