using System.IO.Compression;
using System.Xml.Linq;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// File and directory management utilities for test automation.
    /// </summary>
    public class Files
    {
        /// <summary>
        /// Supported file extensions for file operations.
        /// </summary>
        public enum FileExtension
        {
            xls,
            xlsx,
            doc,
            docx,
            xml
        }

        /// <summary>
        /// Retrieves the most recently modified file from a directory with the specified extension.
        /// </summary>
        /// <param name="directoryPath">The directory path to search for files</param>
        /// <param name="fileExtension">The file extension to filter by</param>
        /// <returns>The FileInfo object of the most recently modified file, or null if no files found</returns>
        public FileInfo GetLatestFile(string directoryPath, FileExtension fileExtension)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            return directory.GetFiles("*." + fileExtension.ToString())
                .OrderByDescending(f => f?.LastWriteTime ?? DateTime.MinValue)
                .FirstOrDefault()!;
        }

        /// <summary>
        /// Verifies if a file exists at the specified path and logs the result.
        /// </summary>
        /// <param name="pathOfFileToVerify">The complete path of the file to verify</param>
        /// <returns>true if the file exists; false if the file does not exist</returns>
        public bool VerifyFilePresent(string pathOfFileToVerify)
        {
            if (File.Exists(pathOfFileToVerify))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Ensures a directory exists, creating it if it doesn't exist.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to ensure exists</param>
        public void EnsureDirectoryExist(string directoryPath)
        {
            try
            {
                if (string.IsNullOrEmpty(directoryPath))
                    throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nEnsureDirectoryExist failed for directoryPath: {directoryPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Verifies if a folder exists at the specified path.
        /// </summary>
        /// <param name="folderPath">The path of the folder to verify</param>
        /// <returns>true if the folder exists; false otherwise</returns>
        public bool VerifyFolderPresent(string folderPath)
        {
            try
            {
                return Directory.Exists(folderPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies a file from the source directory to the target directory.
        /// </summary>
        /// <param name="fileName">The name of the file to copy</param>
        /// <param name="sourcePath">The source directory path</param>
        /// <param name="targetPath">The target directory path where the file will be copied</param>
        public void CopyFile(string fileName, string sourcePath, string targetPath)
        {
            string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
            string destFile = System.IO.Path.Combine(targetPath, fileName);

            // To copy a folder's contents to a new location:
            // Create a new target folder, if necessary.
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // To copy a file to another location and 
            // overwrite the destination file if it already exists.
            System.IO.File.Copy(sourceFile, destFile, true);
        }

        /// <summary>
        /// Move a file from the source directory to the target directory.
        /// </summary>
        /// <param name="fileName">The name of the file to copy</param>
        /// <param name="sourcePath">The source directory path</param>
        /// <param name="targetPath">The target directory path where the file will be copied</param>
        public void MoveFile(string fileName, string sourcePath, string targetPath)
        {
            string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
            string destFile = System.IO.Path.Combine(targetPath, fileName);

            // To copy a folder's contents to a new location:
            // Create a new target folder, if necessary.
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // To copy a file to another location and 
            // overwrite the destination file if it already exists.
            System.IO.File.Move(sourceFile, destFile, true);
        }

        /// <summary>
        /// Determines if a file is currently locked by another process.
        /// </summary>
        /// <param name="file">The FileInfo object representing the file to check</param>
        /// <returns>true if the file is locked or unavailable; false if the file is available</returns>
        public Boolean IsFileLocked(FileInfo file)
        {
            FileStream? stream = null; // Use nullable FileStream to avoid CS8600
            try
            {
                //Don't change FileAccess to ReadWrite, 
                //because if a file is in readOnly, it fails.
                stream = file.Open
                (
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None
                );
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close(); // Use null-conditional operator to safely close the stream
            }

            //file is not locked
            return false;
        }

        /// <summary>
        /// Deletes all files from the specified directory while preserving the directory structure.
        /// </summary>
        /// <param name="pathOfFolder">The directory path from which to delete all files</param>
        public void DeleteAllFilesFromFolder(string pathOfFolder)
        {
            DirectoryInfo dir = new DirectoryInfo(pathOfFolder);
            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }
        }

        /// <summary>
        /// Deletes a single file at the specified path after verifying its existence.
        /// </summary>
        /// <param name="completePathOfFile">The complete path of the file to delete</param>
        public void DeleteFile(string completePathOfFile)
        {
            if (VerifyFilePresent(completePathOfFile))
                File.Delete(completePathOfFile);
        }

        /// <summary>
        /// Creates a new file at the specified path with optional content and directory structure.
        /// </summary>
        /// <param name="completePathOfFile">The complete path where the file should be created</param>
        /// <param name="content">Optional text content to write to the file (defaults to empty string)</param>
        public void CreateFile(string completePathOfFile, string content = "")
        {
            try
            {
                // Create directory if it doesn't exist                
                string? directoryPath = Path.GetDirectoryName(completePathOfFile);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                // Create the file and write content if provided
                File.WriteAllText(completePathOfFile, content);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCreateFile failed.\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Extracts a zip file to the specified directory and cleans up the zip file.
        /// </summary>
        /// <param name="zipFilePath">Path to the zip file</param>
        /// <param name="extractToDirectory">Directory to extract files to</param>
        /// <returns>True if extraction successful, false otherwise</returns>
        public bool ExtractZipFile(string zipFilePath, string extractToDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(zipFilePath))
                    throw new ArgumentException("Zip file path cannot be null or empty", nameof(zipFilePath));

                if (string.IsNullOrEmpty(extractToDirectory))
                    throw new ArgumentException("Extract directory cannot be null or empty", nameof(extractToDirectory));

                if (!File.Exists(zipFilePath))
                    throw new FileNotFoundException($"Zip file not found: {zipFilePath}");

                ZipFile.ExtractToDirectory(zipFilePath, extractToDirectory, true); // true = overwrite existing files 

                Utilities.Files.DeleteFile(zipFilePath);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nExtractZipFile failed for zipFilePath: {zipFilePath} and extractToDirectory: {extractToDirectory}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Zips all files from the specified source directory into the destination .zip file.
        /// If destinationZipPath is null or empty, creates "<sourceDirectory>.zip" next to the source directory.
        /// </summary>
        /// <param name="sourceDirectory">Directory whose contents will be zipped</param>
        /// <param name="destinationZipPath">Optional full path to the output .zip file</param>
        /// <returns>true if zipping succeeds; otherwise false</returns>
        public bool ZipAllFiles(string sourceDirectory, string? destinationZipPath = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                    throw new ArgumentException("Source directory cannot be null or empty", nameof(sourceDirectory));
                if (!Directory.Exists(sourceDirectory))
                    throw new DirectoryNotFoundException($"Directory not found: {sourceDirectory}");

                // Default destination: sibling zip named after the source directory
                if (string.IsNullOrWhiteSpace(destinationZipPath))
                {
                    var parent = Path.GetDirectoryName(sourceDirectory) ?? string.Empty;
                    var folderName = Path.GetFileName(sourceDirectory);
                    destinationZipPath = Path.Combine(parent, $"{folderName}.zip");
                }

                var destDir = Path.GetDirectoryName(destinationZipPath);
                if (string.IsNullOrWhiteSpace(destDir))
                    throw new ArgumentException("Destination directory cannot be determined", nameof(destinationZipPath));
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                // Overwrite existing zip
                if (File.Exists(destinationZipPath))
                    File.Delete(destinationZipPath);

                ZipFile.CreateFromDirectory(sourceDirectory, destinationZipPath, CompressionLevel.Fastest, includeBaseDirectory: false);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nZipAllFiles failed for sourceDirectory: {sourceDirectory} and destinationZipPath: {destinationZipPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Changes the file extension of the specified file from .zip to .sdpack.
        /// </summary>
        /// <param name="filePath">The full path of the .zip file to be renamed.</param>
        public void ChangeZipExtensionToSdpack(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");
            string newFilePath = Path.ChangeExtension(filePath, ".sdpack");
            File.Move(filePath, newFilePath);
        }

        /// <summary>
        /// Renames the specified file to a new file name within the same directory.
        /// </summary>
        /// <param name="filePath">The full path of the file to be renamed.</param>
        /// <param name="newFileName">The new file name.</param>
        public void RenameFileName(string filePath, string newFileName)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");
            string directoryPath = Path.GetDirectoryName(filePath) ?? throw new ArgumentException("Invalid file path", nameof(filePath));
            string newFilePath = Path.Combine(directoryPath, newFileName);
            File.Move(filePath, newFilePath);
        }

        /// <summary>
        /// Writes the specified content to a text file at the given path, creating the file and any necessary directories.
        /// </summary>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="content">The content to write to the file.</param>
        public void WriteTextToFile(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nWriteTextToFile failed for filePath: {filePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL to the local destination path.
        /// </summary>
        /// <param name="url">URL to download from</param>
        /// <param name="destinationPath">Local path to save the file</param>
        /// <param name="timeout">Timeout in minutes for the download operation</param>
        /// <returns>True if download successful, false otherwise</returns>
        public bool DownloadFile(string url, string destinationPath, int timeout = 10)
        {
            try
            {
                string fileName = Path.GetFileName(url);
                string completeFilename = Path.Combine(destinationPath, fileName);

                Utilities.Files.EnsureDirectoryExist(destinationPath);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(timeout);

                var response = httpClient.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                File.WriteAllBytes(completeFilename, fileBytes);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDownloadFile failed for url: {url} and destinationPath: {destinationPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        // replace the value for provided xml file, parentTag, subTag and key
        /// <param name="xmlFilePath"></param>
        /// <param name="parentTag"></param>
        /// <param name="subTag"></param>
        /// <param name="attribute"></param>
        /// <param name="newValue"></param>
        public void ReplaceXMLData(string xmlFilePath, string parentTag, string subTag, string attribute, string newValue)
        {
            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException($"Config file not found: {xmlFilePath}");
            var doc = XDocument.Load(xmlFilePath);
            var parents = doc.Descendants(parentTag).ToList();
            if (parents.Count == 0)
                throw new Exception($"Tag '{parentTag}' not found in XML.");
            bool updated = false;
            foreach (var parent in parents)
            {
                foreach (var table in parent.Descendants(subTag))
                {
                    var atr = table.Element(attribute);
                    if (atr != null)
                    {
                        atr.Value = newValue;
                        updated = true;
                    }
                }
            }
            if (!updated)
                throw new Exception($"Element '{attribute}' not found under any '{subTag}' within '{parentTag}'.");
            doc.Save(xmlFilePath);
        }

        /// <summary>
        /// Retrieves all files from the specified directory that match the given search pattern and option.
        /// </summary>
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        /// <summary>
        /// Returns the file name and extension of the specified path string.
        /// </summary>
        /// <param name="filePath">The path string from which to obtain the file name and extension.</param>
        /// <returns>The file name and extension.</returns>
        public string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        /// <summary>
        /// Checks if the specified directory contains any files with the given extension.
        /// </summary>
        public bool HasFilesWithExtension(string directoryPath, string extension)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(extension))
                return false;
            if (!Directory.Exists(directoryPath))
                return false;
            return Directory.EnumerateFiles(directoryPath, $"*{extension}", SearchOption.TopDirectoryOnly).Any();
        }

        /// <summary>
        /// Retrieves all files from the specified directory with the given extension.
        /// </summary>
        /// <param name="directoryPath">The directory path to search for files</param>
        /// <param name="extension">The file extension to filter by</param>
        /// <returns>An array of file paths for the found files, or an empty array if none found</returns>
        public string[] GetFilesByExtension(string directoryPath, string extension)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                return Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(extension))
                return Array.Empty<string>();
            if (!extension.StartsWith("."))
                extension = "." + extension;
            return Directory.GetFiles(directoryPath, $"*{extension}");
        }
    }
}