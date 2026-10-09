using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;
using System.Collections.Concurrent;
using static HybridAutomation.Helpers.Logger;

namespace HybridAutomation.Helpers.Reporting
{
    /// <summary>
    /// HTML-based test reporting functionality using ExtentReports framework.
    /// </summary>
    public class ExtentLogger
    {
        private ExtentTest? Test;
        private string? FilePath;
        private ConcurrentDictionary<string, ExtentTest> TestMap = new();
        private ExtentReports? Extent;
        private ExtentSparkReporter? SparkReporter;
        private string ClassName;

        /// <summary>
        /// Initializes the ExtentLogger with HTML report file path and test class name.
        /// </summary>
        /// <param name="filePath">The full file path where the HTML report will be generated</param>
        /// <param name="className">The name of the test class being executed</param>
        public ExtentLogger(string filePath, string className)
        {
            FilePath = filePath;
            ClassName = className;
            Extent = GetExtent(className);
        }

        /// <summary>
        /// Initializes logging for a test method by creating an ExtentTest instance.
        /// </summary>
        /// <param name="methodName">The name of the test method being started</param>
        public void StartLogging(string methodName)
        {
            Test = CreateTest(methodName);
        }

        /// <summary>
        /// Logs a message to the HTML report with optional Base64-encoded screenshot.
        /// </summary>
        /// <param name="logType">The type of log message (Info, Pass, Warning, Skip, Error, Fail)</param>
        /// <param name="message">The message content to be logged</param>
        /// <param name="base64String">Optional Base64-encoded screenshot string for Error and Fail log entries</param>
        public void Log(LogType logType, string message, bool isScreenShotNeeded = true, string base64String = "")
        {
            Exception ex = new Exception(message);
            switch (logType)
            {
                case LogType.Info:
                    GetTest().Log(Status.Info, message);
                    break;
                case LogType.Pass:
                    GetTest().Log(Status.Pass, message);
                    break;
                case LogType.Warning:
                    if (!string.IsNullOrEmpty(base64String) && isScreenShotNeeded)
                        GetTest().Log(Status.Warning, ex, MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64String).Build());
                    else
                        GetTest().Log(Status.Warning, ex);
                    break;
                case LogType.Skip:
                    GetTest().Log(Status.Skip, message);
                    break;
                case LogType.Error:
                    if (!string.IsNullOrEmpty(base64String) && isScreenShotNeeded)
                        GetTest().Log(Status.Error, ex, MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64String).Build());
                    else
                        GetTest().Log(Status.Error, ex);
                    break;
                case LogType.Fail:
                    if (!string.IsNullOrEmpty(base64String) && isScreenShotNeeded)
                        GetTest().Log(Status.Fail, ex, MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64String).Build());
                    else
                        GetTest().Log(Status.Fail, ex);
                    break;
            }
        }

        /// <summary>
        /// Creates and configures the ExtentReports instance with SparkReporter for HTML output.
        /// </summary>
        /// <param name="className">The name of the test class used for setting the document title</param>
        /// <returns>The configured ExtentReports instance ready for test logging</returns>
        public ExtentReports GetExtent(string className)
        {
            if (Extent == null)
            {
                SparkReporter = new ExtentSparkReporter(FilePath);
                SparkReporter.Config.Theme = Theme.Dark;
                SparkReporter.Config.DocumentTitle = className;
                //SparkReporter.Config.ReportName = TestContext.CurrentContext.Test.Name;
                Extent = new ExtentReports();
                Extent.AttachReporter(SparkReporter);
                Extent.AddSystemInfo("Environment", "QA");
                Extent.AddSystemInfo("OS", Environment.OSVersion.ToString());
                Extent.AddSystemInfo("User", Environment.UserName);
            }
            return Extent;
        }

        /// <summary>
        /// Creates a new ExtentTest instance for the specified test name and maps it to the current thread.
        /// </summary>
        /// <param name="testName">The name of the test method to be created and tracked</param>
        /// <returns>The created ExtentTest instance mapped to the current thread</returns>
        public ExtentTest CreateTest(string testName)
        {
            var test = Extent?.CreateTest(testName);
            if (test != null)
                TestMap[Thread.CurrentThread.ManagedThreadId.ToString()] = test;
            return test ?? throw new InvalidOperationException("Failed to create an ExtentTest instance.");
        }

        /// <summary>
        /// Retrieves the ExtentTest instance associated with the current thread.
        /// </summary>
        /// <returns>The ExtentTest instance for the current thread</returns>
        public ExtentTest GetTest()
        {
            TestMap.TryGetValue(Thread.CurrentThread.ManagedThreadId.ToString(), out ExtentTest? test);
            return test ?? throw new InvalidOperationException("No ExtentTest instance found for the current thread.");
        }

        /// <summary>
        /// Flushes the ExtentReports instance to write all logged data to the HTML report file.
        /// </summary>
        public void Flush()
        {
            GetExtent(ClassName).Flush();
        }

        /// <summary>
        /// Performs a one-time flush operation to finalize all HTML report data.
        /// </summary>
        public void OneTimeFlush()
        {
            Extent!.Flush();
        }
    }
}
