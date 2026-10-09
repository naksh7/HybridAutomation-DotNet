using HybridAutomation.Helpers;
using HybridAutomation.POM.OnPrem;

namespace HybridAutomation.SmokeTests
{
    public class NotepadTest : TestBase
    {
        [Test]
        public void NotepadWriteTest()
        {           
            Utilities.WinApp.InitializeSession(@"C:\Windows\System32\notepad.exe", "Notepad", Parameters?.WinAppDriverExePath ?? string.Empty, Parameters?.WinAppDriverURI ?? string.Empty);            
            OnPrem.Notepad.TypeText("Hello, this is a test for Notepad application using WinAppDriver.");
            OnPrem.Notepad.SaveFile(@"C:\Users\Public\Documents\TestFile.txt");
        }        
    }
}
