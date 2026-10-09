using HybridAutomation.Helpers;
using OpenQA.Selenium;

namespace HybridAutomation.POM.OnPrem
{
    public class Notepad
    {
        private By TextArea = By.Name("Text editor");
        private By FileMenu = By.Name("File");
        private By Save = By.Name("Save");
        private By FileName = By.Name("File name");

        public void TypeText(string text)
        {
            Utilities.WinApp.SetTextIn(TextArea, text);
        }

        public void SaveFile(string filePath)
        {
            Utilities.WinApp.Click(FileMenu);
            Utilities.WinApp.Click(Save);
            
        }

    }
}
