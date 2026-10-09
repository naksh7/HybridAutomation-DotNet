using HybridAutomation.Helpers.Reporting;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Central orchestrator for multi-format logging system.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Available log levels for message categorization.
        /// </summary>
        public enum LogType
        {
            Info, Pass, Warning, Skip, Error, Fail
        }

        // Static instances of the three logging components for centralized lifecycle management
        private static ExtentLogger? ExtentLogger;
        private static ConsoleLogger? ConsoleLogger;
        private static FileLogger? FileLogger;

        /// <summary>
        /// Initializes multi-format logging infrastructure for test execution.
        /// </summary>
        /// <param name="traceReport">Full file path for the text-based trace log file</param>
        /// <param name="htmlReport">Full file path for the HTML report file</param>
        /// <param name="className">Name of the test class being executed</param>
        public void Initialize(string traceReport, string htmlReport, string className)
        {
            ExtentLogger = new ExtentLogger(htmlReport, className);
            ConsoleLogger = new ConsoleLogger();
            FileLogger = new FileLogger(traceReport, className);
        }

        /// <summary>
        /// Initiates synchronized logging for a test method across all formats.
        /// </summary>
        /// <param name="methodName">Name of the test method being started</param>
        public void StartLogging(string methodName, string testArtefacts)
        {
            ExtentLogger!.StartLogging(methodName);
            ConsoleLogger!.StartLogging(methodName, testArtefacts);
            FileLogger!.StartLogging(methodName);
        }

        /// <summary>
        /// Logs a message to all configured logging formats with optional screenshot.
        /// </summary>
        /// <param name="logType">The log level/type for message categorization</param>
        /// <param name="message">The message content to be logged</param>
        /// <param name="base64EncodedString">Optional Base64-encoded screenshot string</param>
        public void Log(LogType logType, string message, bool isScreenShotNeeded = true, string base64String = "")
        {
            ExtentLogger!.Log(logType, message, isScreenShotNeeded, base64String);
            ConsoleLogger!.Log(logType, message);
            FileLogger!.Log(logType, message);
        }

        /// <summary>
        /// Finalizes test method logging and ensures data persistence.
        /// </summary>
        /// <param name="methodName">Name of the test method being completed</param>
        public void Flush(string methodName)
        {
            FileLogger!.Flush(methodName);
            ConsoleLogger!.Flush(methodName);
            ExtentLogger!.Flush();
        }

        /// <summary>
        /// Performs final cleanup and data persistence for all logging systems.
        /// </summary>
        /// <param name="className">Name of the test class being completed</param>
        public void OneTimeFlush(string className)
        {
            FileLogger!.OneTimeFlush(className);
            ExtentLogger!.OneTimeFlush();
        }
    }
}
