using System.Diagnostics;

namespace HybridAutomation.Helpers
{
#pragma warning disable CA1416
    /// <summary>
    /// Handles environment setup operations including diff-pdf tool installation and configuration.
    /// </summary>
    public class EnvironmentSetup
    {
        /// <summary>
        /// Installs an MSI package silently using msiexec.
        /// </summary>
        /// <param name="msiFilePath">Path to the MSI file to install</param>
        /// <param name="installationPath">Optional installation path (if supported by the MSI)</param>
        /// <param name="additionalParameters">Additional parameters to pass to msiexec</param>
        /// <returns>True if installation successful, false otherwise</returns>
        private bool InstallMsiPackage(string msiFilePath, string? installationPath = null, string additionalParameters = "")
        {
            try
            {
                if (string.IsNullOrEmpty(msiFilePath))
                    throw new ArgumentException("MSI file path cannot be null or empty", nameof(msiFilePath));

                if (!File.Exists(msiFilePath))
                    throw new FileNotFoundException($"MSI file not found: {msiFilePath}");

                // Build msiexec command
                string arguments = $"/i \"{msiFilePath}\" /quiet /norestart";

                // Add installation path if provided
                if (!string.IsNullOrEmpty(installationPath))
                {
                    arguments += $" INSTALLDIR=\"{installationPath}\"";
                }

                // Add additional parameters if provided
                if (!string.IsNullOrEmpty(additionalParameters))
                {
                    arguments += $" {additionalParameters}";
                }

                Utilities.Logger.Log(Logger.LogType.Info, $"Installing MSI package: {msiFilePath}");
                Utilities.Logger.Log(Logger.LogType.Info, $"Command arguments: {arguments}");

                // Execute msiexec with admin privileges
                var result = Utilities.ProcessHandler.ExecuteProcess("msiexec.exe", arguments, runAsAdmin: true, redirectOutput: true);

                if (result.IsExecuted && result.ExitCode == 0)
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"MSI package installed successfully: {msiFilePath}");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Error, $"MSI installation failed. Exit code: {result.ExitCode}");
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        Utilities.Logger.Log(Logger.LogType.Error, $"Error message: {result.ErrorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nInstallMsiPackage failed for msiFilePath: {msiFilePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Refreshes the current process PATH environment variable from system and user variables.
        /// </summary>
        /// <returns>True if refresh was successful, false otherwise</returns>
        private bool RefreshProcessEnvironmentPath()
        {
            try
            {
                // Get system-wide PATH
                string systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;

                // Get user-level PATH
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;

                // Combine system and user PATH (system comes first)
                string combinedPath = string.IsNullOrEmpty(systemPath)
                    ? userPath
                    : string.IsNullOrEmpty(userPath)
                        ? systemPath
                        : $"{systemPath};{userPath}";

                // Set the combined PATH for the current process
                Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);

                Utilities.Logger.Log(Logger.LogType.Info, "Process PATH environment variable refreshed successfully");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRefreshProcessEnvironmentPath failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Removes the specified path from system-wide environment variables.
        /// </summary>
        /// <param name="folderPath">Path to remove from environment variable</param>
        /// <returns>True if removal successful, false otherwise</returns>
        private bool RemoveFromEnvironmentVariable(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

                if (!Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, "Not running with administrative privileges, attempting to elevate for cleanup");
                    throw new System.Security.SecurityException("Administrative privileges required for cleanup");
                }

                string currentSystemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;

                if (currentSystemPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Any(p => p.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    var pathEntries = currentSystemPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => !p.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    string newSystemPath = string.Join(";", pathEntries);

                    Environment.SetEnvironmentVariable("PATH", newSystemPath, EnvironmentVariableTarget.Machine);
                    Utilities.Logger.Log(Logger.LogType.Info, "Path removed from system environment variable successfully");
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Info, "Path not found in system environment variable");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRemoveFromEnvironmentVariable failed for folderPath: {folderPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sets up environment variable to include the diff-pdf tool path.
        /// </summary>
        /// <param name="diffPdfDirectory">Directory containing diff-pdf.exe</param>
        /// <returns>True if environment variable setup successful, false otherwise</returns>
        private bool SetupEnvironmentVariable(string diffPdfDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(diffPdfDirectory))
                    throw new ArgumentException("Directory path cannot be null or empty", nameof(diffPdfDirectory));

                // Get current system PATH environment variable
                string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;

                // Check if the path is already in the environment variable
                if (currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Any(p => p.Equals(diffPdfDirectory, StringComparison.OrdinalIgnoreCase)))
                {
                    Utilities.Logger.Log(Logger.LogType.Info, "Path already exists in system environment variable");
                    // Still refresh the process environment to ensure it's available
                    RefreshProcessEnvironmentPath();
                    return true;
                }

                // Add the new path to the environment variable
                string newPath = string.IsNullOrEmpty(currentPath)
                    ? diffPdfDirectory
                    : $"{currentPath};{diffPdfDirectory}";

                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);

                Utilities.Logger.Log(Logger.LogType.Info, "System-wide environment variable updated");

                // Refresh the current process PATH to pick up the changes immediately
                RefreshProcessEnvironmentPath();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetupEnvironmentVariable failed for diffPdfDirectory: {diffPdfDirectory}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if diff-pdf command is available in the system PATH.
        /// </summary>
        /// <returns>True if command is available, false otherwise</returns>
        private bool IsDiffPDFAvailable()
        {
            try
            {
                RefreshProcessEnvironmentPath();
                using var process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c diff-pdf a.pdf b.pdf";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                return !error.Contains("is not recognized as an internal or external command", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nIsDiffPDFAvailable failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if Windows Developer Mode is currently enabled.
        /// </summary>
        /// <returns>True if Developer Mode is enabled, false otherwise</returns>
        private bool IsDeveloperModeEnabled()
        {
            try
            {
                // Registry path for Windows Developer Mode settings
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
                const string allowDevelopmentWithoutDevLicense = "AllowDevelopmentWithoutDevLicense";

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath, false))
                {
                    if (key == null)
                    {
                        Utilities.Logger.Log(Logger.LogType.Info, "Developer Mode registry key not found - Developer Mode is disabled");
                        return false;
                    }

                    var value = key.GetValue(allowDevelopmentWithoutDevLicense);
                    bool isEnabled = value != null && Convert.ToInt32(value) == 1;

                    string status = isEnabled ? "enabled" : "disabled";
                    Utilities.Logger.Log(Logger.LogType.Info, $"Developer Mode is currently {status}");

                    return isEnabled;
                }
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(Logger.LogType.Warning, $"Failed to check Developer Mode status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets up diff-pdf tool with system-wide installation and administrative privilege handling.
        /// </summary>
        /// <param name="downloadUrl">URL to download diff-pdf.zip from</param>
        /// <param name="folderPath">Path where diff-pdf tool will be installed</param>
        /// <returns>True if setup completed successfully, false otherwise</returns>
        public bool SetupDiffPdfToolSystemWide(string downloadUrl, string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(downloadUrl))
                    throw new ArgumentException("Download URL cannot be null or empty", nameof(downloadUrl));

                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

                if (!IsDiffPDFAvailable())
                {
                    // Check if already running as administrator
                    if (Utilities.ProcessHandler.IsRunningAsAdministrator())
                    {
                        Utilities.Logger.Log(Logger.LogType.Info, "Running with administrative privileges, proceeding with system-wide setup");


                        string zipFilePath = Path.Combine(folderPath, Path.GetFileName(downloadUrl));
                        string extractedExePath = Path.Combine(folderPath, "diff-pdf.exe");

                        Utilities.Logger.Log(Logger.LogType.Info, $"Starting diff-pdf setup from URL: {downloadUrl}");

                        // Create the PDFDifference directory
                        Utilities.Files.EnsureDirectoryExist(folderPath);

                        // Download the diff-pdf.zip file
                        if (!Utilities.Files.DownloadFile(downloadUrl, folderPath))
                        {
                            Utilities.Logger.Log(Logger.LogType.Error, "Failed to download diff-pdf.zip");
                            return false;
                        }

                        // Extract the zip file
                        if (!Utilities.Files.ExtractZipFile(zipFilePath, folderPath))
                        {
                            Utilities.Logger.Log(Logger.LogType.Error, "Failed to extract diff-pdf.zip");
                            return false;
                        }

                        // Set up environment variable
                        if (!SetupEnvironmentVariable(folderPath))
                        {
                            Utilities.Logger.Log(Logger.LogType.Error, "Failed to set up environment variable");
                            return false;
                        }

                        // Verify diff-pdf.exe exists
                        if (Utilities.Files.VerifyFilePresent(extractedExePath))
                        {
                            Utilities.Logger.Log(Logger.LogType.Info, $"diff-pdf tool set up at: {extractedExePath}");
                            return true;
                        }
                        else
                        {
                            Utilities.Logger.Log(Logger.LogType.Error, $"diff-pdf.exe not found at expected location: {extractedExePath}");
                            return false;
                        }
                    }
                    else
                    {
                        Utilities.Logger.Log(Logger.LogType.Warning, "Not running with administrative privileges, attempting to elevate for system-wide setup");
                        throw new System.Security.SecurityException("Administrative privileges required for system-wide installation");
                    }
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Info, "diff-pdf.exe already installed and Environment variable is also set");
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetupDiffPdfToolSystemWide failed for downloadUrl: {downloadUrl} and folderPath: {folderPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Removes the diff-pdf tool and cleans up the installation including environment variables.
        /// </summary>
        /// <param name="folderPath">Path to the diff-pdf tool installation folder</param>
        /// <returns>True if cleanup successful, false otherwise</returns>
        public bool CleanupDiffPdfTool(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

                if (Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    // Remove the directory and all its contents
                    if (Utilities.Files.VerifyFolderPresent(folderPath))
                    {
                        Utilities.Files.DeleteAllFilesFromFolder(folderPath);
                        Utilities.Logger.Log(Logger.LogType.Info, $"Removed directory: {folderPath}");
                    }
                    else
                    {
                        Utilities.Logger.Log(Logger.LogType.Info, $"Directory does not exist: {folderPath}");
                    }

                    // Remove from environment variable
                    RemoveFromEnvironmentVariable(folderPath);

                    Utilities.Logger.Log(Logger.LogType.Info, "diff-pdf tool cleanup completed successfully");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, "Not running with administrative privileges, attempting to elevate for cleanup");
                    throw new System.Security.SecurityException("Administrative privileges required for cleanup");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCleanupDiffPdfTool failed for folderPath: {folderPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Sets up WinAppDriver by downloading and installing the MSI package silently.
        /// </summary>
        /// <param name="url">URL to download the WinAppDriver MSI from</param>
        /// <param name="downloadPath">Path where the MSI file will be downloaded</param>
        /// <param name="installationPath">Installation path for WinAppDriver (optional)</param>
        /// <returns>True if setup completed successfully, false otherwise</returns>
        public bool SetUpWinAppDriver(string url, string downloadPath, string installationPath = @"C:\Program Files (x86)\Windows Application Driver")
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    throw new ArgumentException("URL cannot be null or empty", nameof(url));

                if (string.IsNullOrEmpty(downloadPath))
                    throw new ArgumentException("Download path cannot be null or empty", nameof(downloadPath));

                // Check if WinAppDriver is already installed
                string winAppDriverExe = Path.Combine(installationPath, "WinAppDriver.exe");
                if (Utilities.Files.VerifyFilePresent(winAppDriverExe))
                {
                    Utilities.Logger.Log(Logger.LogType.Info, "WinAppDriver is already installed");
                    return true;
                }

                // Check if running as administrator (required for MSI installation)
                if (!Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, "Administrative privileges required for WinAppDriver installation");
                    throw new System.Security.SecurityException("Administrative privileges required for MSI installation");
                }

                Utilities.Logger.Log(Logger.LogType.Info, $"Starting WinAppDriver setup from URL: {url}");

                // Ensure download directory exists
                Utilities.Files.EnsureDirectoryExist(downloadPath);

                // Construct full file path for the downloaded MSI
                string fullFilePath = Path.Combine(downloadPath, Path.GetFileName(url));

                // Download the MSI file
                if (!Utilities.Files.DownloadFile(url, downloadPath))
                {
                    Utilities.Logger.Log(Logger.LogType.Error, "Failed to download WinAppDriver MSI");
                    return false;
                }

                // Verify the downloaded file exists
                if (!Utilities.Files.VerifyFilePresent(fullFilePath))
                {
                    Utilities.Logger.Log(Logger.LogType.Error, $"Downloaded MSI file not found: {fullFilePath}");
                    return false;
                }

                // Install the MSI package silently
                if (!InstallMsiPackage(fullFilePath, installationPath))
                {
                    Utilities.Logger.Log(Logger.LogType.Error, "Failed to install WinAppDriver MSI");
                    return false;
                }

                // Verify installation by checking if WinAppDriver.exe exists
                if (Utilities.Files.VerifyFilePresent(winAppDriverExe))
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"WinAppDriver installed successfully at: {installationPath}");

                    // Clean up downloaded MSI file
                    try
                    {
                        Utilities.Files.DeleteFile(fullFilePath);
                        Utilities.Logger.Log(Logger.LogType.Info, "Cleaned up downloaded MSI file");
                    }
                    catch (Exception ex)
                    {
                        Utilities.Logger.Log(Logger.LogType.Warning, $"Failed to clean up MSI file: {ex.Message}");
                        // Don't fail the entire operation for cleanup issues
                    }

                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Error, $"WinAppDriver.exe not found after installation at: {winAppDriverExe}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSetUpWinAppDriver failed for url: {url} and downloadPath: {downloadPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Enables Windows Developer Mode by modifying the registry settings.
        /// Developer Mode allows sideloading of apps and enables developer features.
        /// </summary>
        /// <returns>True if Developer Mode was enabled successfully, false otherwise</returns>
        public bool EnableDeveloperMode()
        {
            try
            {
                // Check if running as administrator (required for registry modification)
                if (!Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, "Administrative privileges required to enable Developer Mode");
                    throw new System.Security.SecurityException("Administrative privileges required to modify registry settings");
                }

                Utilities.Logger.Log(Logger.LogType.Info, "Starting Windows Developer Mode activation");

                // Registry path for Windows Developer Mode settings
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
                const string allowDevelopmentWithoutDevLicense = "AllowDevelopmentWithoutDevLicense";
                const string allowAllTrustedApps = "AllowAllTrustedApps";

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath, true))
                {
                    if (key == null)
                    {
                        Utilities.Logger.Log(Logger.LogType.Error, $"Registry key not found: HKEY_LOCAL_MACHINE\\{registryPath}");
                        return false;
                    }

                    // Enable Developer Mode (AllowDevelopmentWithoutDevLicense = 1)
                    key.SetValue(allowDevelopmentWithoutDevLicense, 1, Microsoft.Win32.RegistryValueKind.DWord);
                    Utilities.Logger.Log(Logger.LogType.Info, "Set AllowDevelopmentWithoutDevLicense = 1");

                    // Enable sideloading of apps (AllowAllTrustedApps = 1)
                    key.SetValue(allowAllTrustedApps, 1, Microsoft.Win32.RegistryValueKind.DWord);
                    Utilities.Logger.Log(Logger.LogType.Info, "Set AllowAllTrustedApps = 1");
                }

                // Verify the settings were applied correctly
                if (IsDeveloperModeEnabled())
                {
                    Utilities.Logger.Log(Logger.LogType.Pass, "Windows Developer Mode enabled successfully");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Error, "Failed to enable Developer Mode - verification check failed");
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception($"Access denied when modifying registry. Ensure you're running as administrator.\n{ex.Message}\nEnableDeveloperMode failed\n{ex.StackTrace}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nEnableDeveloperMode failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Disables Windows Developer Mode by modifying the registry settings.
        /// </summary>
        /// <returns>True if Developer Mode was disabled successfully, false otherwise</returns>
        public bool DisableDeveloperMode()
        {
            try
            {
                // Check if running as administrator (required for registry modification)
                if (!Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, "Administrative privileges required to disable Developer Mode");
                    throw new System.Security.SecurityException("Administrative privileges required to modify registry settings");
                }

                Utilities.Logger.Log(Logger.LogType.Info, "Starting Windows Developer Mode deactivation");

                // Registry path for Windows Developer Mode settings
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
                const string allowDevelopmentWithoutDevLicense = "AllowDevelopmentWithoutDevLicense";
                const string allowAllTrustedApps = "AllowAllTrustedApps";

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath, true))
                {
                    if (key == null)
                    {
                        Utilities.Logger.Log(Logger.LogType.Info, "Registry key not found - Developer Mode is likely already disabled");
                        return true;
                    }

                    // Disable Developer Mode (AllowDevelopmentWithoutDevLicense = 0)
                    key.SetValue(allowDevelopmentWithoutDevLicense, 0, Microsoft.Win32.RegistryValueKind.DWord);
                    Utilities.Logger.Log(Logger.LogType.Info, "Set AllowDevelopmentWithoutDevLicense = 0");

                    // Disable sideloading of apps (AllowAllTrustedApps = 0)
                    key.SetValue(allowAllTrustedApps, 0, Microsoft.Win32.RegistryValueKind.DWord);
                    Utilities.Logger.Log(Logger.LogType.Info, "Set AllowAllTrustedApps = 0");
                }

                // Verify the settings were applied correctly
                if (!IsDeveloperModeEnabled())
                {
                    Utilities.Logger.Log(Logger.LogType.Pass, "Windows Developer Mode disabled successfully");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Error, "Failed to disable Developer Mode - verification check failed");
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception($"Access denied when modifying registry. Ensure you're running as administrator.\n{ex.Message}\nDisableDeveloperMode failed\n{ex.StackTrace}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDisableDeveloperMode failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Starts the audio service if it is not already running.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool StartAudioService()
        {
            try
            {
                var statusResult = Utilities.ProcessHandler.ExecuteProcess("cmd.exe", "/c sc query audiosrv");
                if (statusResult.IsExecuted)
                {
                    string output = statusResult.OutputMessage;
                    if (output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // If not running, attempt to start it
                if (Utilities.ProcessHandler.IsRunningAsAdministrator())
                {
                    Utilities.ProcessHandler.ExecuteProcess("cmd.exe", "/c net start audiosrv");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Skip, "Admin Permission needed to start the service. Skipped the SpeakText");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nStartAudioService failed\n{ex.StackTrace}", ex);
            }
        }

    }
}
