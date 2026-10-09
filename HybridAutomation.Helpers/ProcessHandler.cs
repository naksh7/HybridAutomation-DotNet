using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HybridAutomation.Helpers
{
#pragma warning disable CA1416
    /// <summary>
    /// Process and window management functionality for test automation.
    /// </summary>
    public class ProcessHandler
    {
        /// <summary>
        /// Sets the specified window as the active window using Windows API.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int SetActiveWindow(IntPtr hWnd);

        /// <summary>
        /// Brings the specified window to the foreground and activates it using Windows API.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves the handle of the window that is currently in the foreground.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Brings the specified application to the foreground and activates its main window.
        /// </summary>
        /// <param name="nameOfApplication">The process name of the application to bring to the front</param>
        public void BringApplicationToFront(string nameOfApplication)
        {
            try
            {
                Process[] process = Process.GetProcessesByName(nameOfApplication);

                for (int i = 0; i <= process.Length - 1; i++)
                {
                    IntPtr hWnd = process[i].MainWindowHandle;
                    SetForegroundWindow(hWnd);
                    IntPtr handle = GetForegroundWindow();
                    if (hWnd == handle)
                    {
                        Utilities.Logger.Log(Logger.LogType.Info, $"Set the application {nameOfApplication} to Foreground");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nBringApplicationToFront failed for nameOfApplication: {nameOfApplication}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Terminates all running processes with the specified name.
        /// </summary>
        /// <param name="processName">The name of the process to terminate</param>
        public void EndProcess(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length != 0)
                {
                    foreach (Process p in processes)
                    {

                        p.CloseMainWindow();

                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit();
                        }
                    }
                }
                Utilities.Logger.Log(Logger.LogType.Info, $"{processName} process terminated");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nEndProcess failed for processName: {processName}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Retrieves the main window title of the first found process with the specified name.
        /// </summary>
        /// <param name="processName">The name of the process to search for</param>
        /// <returns>The main window title of the process if found; null if no process exists</returns>
        public string GetFileNameFromProcess(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"No process with given name {processName} found");
                    return string.Empty; // Return an empty string instead of null
                }
                else
                {
                    foreach (Process p in processes)
                    {
                        return p.MainWindowTitle ?? string.Empty; // Ensure a non-null return value
                    }
                }
                return string.Empty; // Return an empty string as a fallback
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetFileNameFromProcess failed for processName: {processName}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Waits for a process to become visible with a main window handle, with a 20-second timeout.
        /// </summary>
        /// <param name="processName">The name of the process to wait for</param>
        public void WaitTillProcessIsVisible(string processName)
        {
            try
            {
                int count = 0;
                while (true)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        Process[] processes = Process.GetProcessesByName(processName);
                        if (processes[0].MainWindowHandle != IntPtr.Zero)
                            break;

                        if (count == 20)
                        {
                            throw new TimeoutException($"Waited for 20 sec. {processName} Not Visible.");
                        }
                        count++;
                    }
                    catch (Exception)
                    {
                        //Increasing count for every attemp of checking processes
                        if (count == 20)
                        {
                            throw new TimeoutException($"Waited for 20 sec. {processName} not Visible.");
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWaitTillProcessIsVisible failed for processName: {processName}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the main window handle of an existing process by its name.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetExistingProcessHandle(string processName)
        {
            try
            {
                Process? process = Process.GetProcessesByName(processName).FirstOrDefault();
                if (process == null)
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"No process with the name {processName} was found.");
                    return string.Empty; // Return an empty string if no process is found
                }
                return process.MainWindowHandle.ToString("x");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetExistingProcessHandle failed for processName: {processName}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Executes a process with specified parameters and returns execution status, exit code, and error message.
        /// </summary>
        /// <param name="fileName">The name of the file to execute</param>
        /// <param name="arguments">The arguments to pass to the process</param>
        /// <param name="runAsAdmin">Whether to run the process as an administrator</param>
        /// <param name="redirectOutput">Whether to redirect the output and error streams</param>
        /// <returns>A tuple containing the execution status, exit code, and error message</returns>
        public (bool IsExecuted, int ExitCode, string OutputMessage, string ErrorMessage) ExecuteProcess(string fileName, string? arguments = null, bool runAsAdmin = true, bool redirectOutput = true)
        {
            try
            {
                string output = string.Empty;
                string error = string.Empty;
                int exitCode = 0;

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments ?? string.Empty;
                    process.StartInfo.UseShellExecute = !redirectOutput;
                    process.StartInfo.RedirectStandardOutput = redirectOutput;
                    process.StartInfo.RedirectStandardError = redirectOutput;
                    process.StartInfo.CreateNoWindow = redirectOutput;

                    if (runAsAdmin)
                        process.StartInfo.Verb = "runas";

                    process.Start();

                    if (redirectOutput)
                    {
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        exitCode = process.ExitCode;

                        if (!string.IsNullOrEmpty(error))
                            return (true, process.ExitCode, output, error);
                    }

                    Utilities.Logger.Log(Logger.LogType.Info, $"Executed process: {fileName} {arguments}");
                    return (true, exitCode, output, error);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nExecuteProcess failed for file: {fileName} {arguments}.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if the current process is running with administrative privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                Utilities.Logger.Log(Logger.LogType.Info, $"Running as administrator: {isAdmin}");
                return isAdmin;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nIsRunningAsAdministrator failed\n{ex.StackTrace}", ex);
            }
        }
    }
}
