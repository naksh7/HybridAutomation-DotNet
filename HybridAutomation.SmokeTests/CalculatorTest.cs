using HybridAutomation.Helpers;
using HybridAutomation.POM.OnPrem;

namespace HybridAutomation.SmokeTests
{
    public class CalculatorTest : TestBase
    {
        [Test]
        public void CalculatorAdditionTest()
        {
            Utilities.WinApp.InitializeSession(@"C:\Windows\System32\calc.exe", "ApplicationFrameHost", Parameters?.WinAppDriverExePath ?? string.Empty, Parameters?.WinAppDriverURI ?? string.Empty);
            OnPrem.Calculator.ClickButton("One");
            OnPrem.Calculator.ClickButton("Plus");
            OnPrem.Calculator.ClickButton("Two");
            OnPrem.Calculator.ClickButton("Equals");
            var result = OnPrem.Calculator.GetResultText();
            Assert.That(result, Is.EqualTo("3"), "The calculator addition result is incorrect.");
            Utilities.Logger.Log(Logger.LogType.Info, "Calculation result: " + result);
        }
    }
}
