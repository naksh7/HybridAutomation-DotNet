using static HybridAutomation.Helpers.Logger;

namespace HybridAutomation.Helpers.Reporting
{
    /// <summary>
    /// File-based logging functionality for test execution with timestamped entries.
    /// </summary>
    public class FileLogger
    {
        private static StreamWriter? fileWriter;
        private string? fileName;

        /// <summary>
        /// Initializes the FileLogger with the specified file path and test class name.
        /// </summary>
        /// <param name="fileName">The full file path where the log file will be created</param>
        /// <param name="className">The name of the test class being executed</param>
        public FileLogger(string fileName, string className)
        {
            this.fileName = fileName;
            string? directoryPath = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directoryPath))
                Utilities.Files.EnsureDirectoryExist(directoryPath);
            fileWriter = new StreamWriter(fileName);
            fileWriter.WriteLine(string.Format("==================== START TEST CLASS: {0} ====================", className));
        }

        /// <summary>
        /// Marks the beginning of a test method execution in the log file.
        /// </summary>
        /// <param name="methodName">The name of the test method being started</param>
        public void StartLogging(string methodName)
        {
            fileWriter!.WriteLine(string.Format("\n-------------------- START TEST METHOD: {0} --------------------", methodName));
        }

        /// <summary>
        /// Writes a timestamped log entry to the file with the specified log type and message.
        /// </summary>
        /// <param name="logType">The type of log message (Info, Pass, Warning, Skip, Error, Fail)</param>
        /// <param name="message">The message content to be logged to the file</param>
        public void Log(LogType logType, string message)
        {
            string logEntry = $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - [{logType}] - {message}";
            fileWriter!.WriteLine(logEntry);
        }

        /// <summary>
        /// Marks the end of a test method execution in the log file.
        /// </summary>
        /// <param name="methodName">The name of the test method being completed</param>
        public void Flush(string methodName)
        {
            fileWriter!.WriteLine($"-------------------- END TEST METHOD : {methodName} ---------------------");
        }

        /// <summary>
        /// Finalizes the log file by writing a test class end marker and flushing all data to disk.
        /// </summary>
        /// <param name="className">The name of the test class being completed</param>
        public void OneTimeFlush(string className)
        {
            fileWriter!.WriteLine($"\n==================== END TEST CLASS : {className} =====================");
            fileWriter.Flush();
        }
    }
}
