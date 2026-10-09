namespace HybridAutomation.POM.OnPrem
{
    public class OnPrem
    {
        #region Central
        private static Notepad? _notepad;
        public static Notepad Notepad => _notepad ??= new Notepad();    
        private static Calculator? _calculator;
        public static Calculator Calculator => _calculator ??= new Calculator();
        #endregion

        /// <summary>
        /// Resets all page object instances. Useful for test cleanup or when starting fresh sessions.
        /// </summary>
        public static void ResetAll()
        {
            _notepad = null;           
        }
    }
}
