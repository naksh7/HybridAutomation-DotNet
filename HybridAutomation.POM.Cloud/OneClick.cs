namespace HybridAutomation.POM.Cloud
{
    public class OneClick
    {
        private static Login? _login;
        private static Home? _home;

        public static Login Login => _login ??= new Login();  
        public static Home Home => _home ??= new Home();
    }
}
