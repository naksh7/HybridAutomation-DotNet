using HybridAutomation.Helpers;

namespace HybridAutomation.POM.Cloud
{
    public class LoginDetails
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class Login
    {
        private string Username = "#username";
        private string Password = "#inputPassword";
        private string Continue = "#buttonContinue";
        private string LogIn = "#buttonLogin";

        public void LoginToOneClick(LoginDetails details)
        {
            if (!string.IsNullOrEmpty(details.Username))
            {
                Utilities.Playwright.SetText(Username, details.Username);
            }            
            Utilities.Playwright.Click(Continue);
            if(!string.IsNullOrEmpty(details.Password)) 
            {
                Utilities.Playwright.SetText(Password, details.Password);
            }           
            Utilities.Playwright.Click(LogIn);
        }
    }
}
