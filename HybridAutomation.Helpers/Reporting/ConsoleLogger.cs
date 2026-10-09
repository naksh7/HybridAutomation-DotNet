using static HybridAutomation.Helpers.Logger;

namespace HybridAutomation.Helpers.Reporting
{
    /// <summary>
    /// Console-based logging functionality for real-time test execution feedback.
    /// </summary>
    public class ConsoleLogger
    {
        /// <summary>
        /// Logs a message to the console with timestamp and log level formatting.
        /// </summary>
        /// <param name="logType">The type of log message (Info, Pass, Warning, Skip, Error, Fail)</param>
        /// <param name="message">The message content to be logged</param>
        public void Log(LogType logType, string message)
        {
            switch (logType)
            {
                case LogType.Info:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [INFO] {message}");
                    break;
                case LogType.Pass:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [PASS] {message}");
                    break;
                case LogType.Warning:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [WARNING] {message}");
                    break;
                case LogType.Skip:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [SKIP] {message}");
                    break;
                case LogType.Error:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [ERROR] {message}");
                    break;
                case LogType.Fail:
                    Console.WriteLine($"{DateTime.Now:dd-MMM-yyyy hh:mm:ss} - [FAIL] {message}");
                    break;
            }
        }

        /// <summary>
        /// Outputs a formatted header to mark the beginning of a test method execution.
        /// </summary>
        /// <param name="methodName">The name of the test method being started</param>
        public void StartLogging(string methodName, string testArtefacts)
        {
            Console.WriteLine($"Test Artefacts can be found at {testArtefacts}");
            Console.WriteLine($"\n-------------------- START TEST METHOD : {methodName} --------------------");
        }

        /// <summary>
        /// Marks the end of a test method execution in the Console.
        /// </summary>
        /// <param name="methodName">The name of the test method being completed</param>
        public void Flush(string methodName)
        {
            Console.WriteLine($"-------------------- END TEST METHOD : {methodName} ----------------------");
        }
    }
}
