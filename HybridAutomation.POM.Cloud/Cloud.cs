namespace HybridAutomation.POM.Cloud
{
    public class Cloud
    {
        private static Google? _google;       

        public static Google Google => _google ??= new Google();

        /// <summary>
        /// Resets all page object instances. Useful for test cleanup or when starting fresh sessions.
        /// </summary>
        public static void ResetAll()
        {
            _google = null;
        }
    }
}
