using OfficeOpenXml;
using static HybridAutomation.Helpers.Logger;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Excel data management utilities for test automation.
    /// </summary>
    public class Excel
    {
        private static Dictionary<string, ScreenData> result = new();
        private static ExcelWorksheet? ExcelWorksheet;
        private static Dictionary<string, int> headers = new Dictionary<string, int>();

        public class InputDetail
        {
            public string? InputNavigation { get; set; }
            public string? InputField { get; set; }
            public string? InputValue { get; set; }
        }

        public class VerificationDetail
        {
            public string? VerificationNavigation { get; set; }
            public string? VerificationField { get; set; }
            public string? VerificationValue { get; set; }
        }

        public class ScreenData
        {
            public List<InputDetail> InputDetails { get; set; } = new();
            public List<VerificationDetail> VerificationDetails { get; set; } = new();
        }

        /// <summary>
        /// Loads Excel data from the specified file and maps it to screen data structure.
        /// </summary>
        /// <param name="filePath">Path to the Excel file to load</param>
        /// <param name="worksheetName">Optional name of the specific worksheet to load. If empty, loads the first worksheet</param>
        public void LoadExcelAndMapData(string filePath, string worksheetName = "")
        {
            try
            {
                if (!Utilities.Files.VerifyFilePresent(filePath))
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");

                result = new Dictionary<string, ScreenData>();
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    // If worksheetName is provided, find the worksheet by name
                    if (!string.IsNullOrEmpty(worksheetName))
                    {
                        worksheet = package.Workbook.Worksheets[worksheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        }
                    }
                    else
                    {
                        // Default to first worksheet if no name is provided
                        worksheet = package.Workbook.Worksheets[0];
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;

                    // Create a dictionary to map column names to their indices
                    var columnMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    // Read header row (row 1) to build column mapping
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            columnMapping[headerValue] = col;
                        }
                    }

                    // Log discovered columns for debugging
                    Utilities.Logger.Log(LogType.Info, $"Excel columns discovered: {string.Join(", ", columnMapping.Keys)}");

                    // Validate that required columns exist
                    var requiredColumns = new[] { "Screen", "Input Navigation", "Input Field", "Input Value",
                                                "Verification Navigation", "Verification Field", "Verification Value" };
                    var missingColumns = requiredColumns.Where(col => !columnMapping.ContainsKey(col)).ToList();

                    if (missingColumns.Any())
                    {
                        var availableColumns = string.Join(", ", columnMapping.Keys);
                        throw new InvalidOperationException($"Missing required columns: {string.Join(", ", missingColumns)}. Available columns: {availableColumns}");
                    }

                    // Get column indices for required fields
                    int screenNameCol = columnMapping["Screen"];
                    int inputNavCol = columnMapping["Input Navigation"];
                    int inputFieldCol = columnMapping["Input Field"];
                    int inputValueCol = columnMapping["Input Value"];
                    int verificationNavCol = columnMapping["Verification Navigation"];
                    int verificationFieldCol = columnMapping["Verification Field"];
                    int verificationValueCol = columnMapping["Verification Value"];

                    string currentScreen = string.Empty;

                    for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip headers)
                    {
                        var screen = worksheet.Cells[row, screenNameCol].Text.Trim();
                        var inputNav = worksheet.Cells[row, inputNavCol].Text.Trim();
                        var inputField = worksheet.Cells[row, inputFieldCol].Text.Trim();
                        var inputValue = worksheet.Cells[row, inputValueCol].Text.Trim();

                        var verificationNav = worksheet.Cells[row, verificationNavCol].Text.Trim();
                        var verificationField = worksheet.Cells[row, verificationFieldCol].Text.Trim();
                        var verificationValue = worksheet.Cells[row, verificationValueCol].Text.Trim();

                        if (!string.IsNullOrEmpty(screen))
                        {
                            currentScreen = screen;
                            if (!result.ContainsKey(currentScreen))
                            {
                                result[currentScreen] = new ScreenData();
                            }
                        }

                        if (string.IsNullOrEmpty(currentScreen)) continue;

                        if (!string.IsNullOrEmpty(inputNav) || !string.IsNullOrEmpty(inputField) || !string.IsNullOrEmpty(inputValue))
                        {
                            result[currentScreen].InputDetails.Add(new InputDetail
                            {
                                InputNavigation = inputNav,
                                InputField = inputField,
                                InputValue = inputValue
                            });
                        }

                        if (!string.IsNullOrEmpty(verificationNav) || !string.IsNullOrEmpty(verificationField) || !string.IsNullOrEmpty(verificationValue))
                        {
                            result[currentScreen].VerificationDetails.Add(new VerificationDetail
                            {
                                VerificationNavigation = verificationNav,
                                VerificationField = verificationField,
                                VerificationValue = verificationValue
                            });
                        }
                    }

                    Utilities.Logger.Log(LogType.Info, $"Successfully loaded Excel data from '{filePath}' with {result.Count} screen(s)");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nLoadExcelAndMapData failed for filePath: {filePath} and worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Retrieves the input value for a specific field within a given screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to search for</param>
        /// <param name="inputField">The input field name to find the value for</param>
        /// <returns>The input value associated with the specified screen and field, or null if not found</returns>
        public string? GetInputValueByScreenAndField(string screenKey, string inputField)
        {
            // Check if screen data exists
            if (!result.TryGetValue(screenKey, out var screenData))
                throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

            // Find the input detail - optimized lookup
            foreach (var detail in screenData.InputDetails)
            {
                if (string.Equals(detail.InputField, inputField, StringComparison.Ordinal))
                {
                    return detail.InputValue?.Trim();
                }
            }

            // Field not found - provide helpful error message
            var availableFields = screenData.InputDetails
                .Where(d => !string.IsNullOrEmpty(d.InputField))
                .Select(d => d.InputField)
                .Distinct()
                .ToList();

            var fieldList = availableFields.Count > 0
                ? $" Available input fields: {string.Join(", ", availableFields)}"
                : " No input fields found in this screen.";

            throw new KeyNotFoundException($"Input field '{inputField}' not found for Screen: '{screenKey}'.{fieldList}");
        }

        /// <summary>
        /// Retrieves the verification value for a specific field within a given screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to search for</param>
        /// <param name="verificationField">The verification field name to find the value for</param>
        /// <returns>The verification value associated with the specified screen and field, or null if not found</returns>
        public string? GetVerificationValueByScreenAndField(string screenKey, string verificationField)
        {
            // Check if screen data exists
            if (!result.TryGetValue(screenKey, out var screenData))
                throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

            // Find the verification detail - optimized lookup
            foreach (var detail in screenData.VerificationDetails)
            {
                if (string.Equals(detail.VerificationField, verificationField, StringComparison.Ordinal))
                {
                    return detail.VerificationValue?.Trim();
                }
            }

            // Field not found - provide helpful error message
            var availableFields = screenData.VerificationDetails
                .Where(d => !string.IsNullOrEmpty(d.VerificationField))
                .Select(d => d.VerificationField)
                .Distinct()
                .ToList();

            var fieldList = availableFields.Count > 0
                ? $" Available verification fields: {string.Join(", ", availableFields)}"
                : " No verification fields found in this screen.";

            throw new KeyNotFoundException($"Verification field '{verificationField}' not found for Screen: '{screenKey}'.{fieldList}");
        }

        /// <summary>
        /// Retrieves the input navigation path for a given screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to get the navigation path for</param>
        /// <returns>The input navigation path for the specified screen, or null if not found</returns>
        public string? GetInputNavigationByScreen(string screenKey)
        {
            // Check if screen data exists
            if (!result.TryGetValue(screenKey, out var screenData))
                throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

            // Find the first input detail with navigation - optimized lookup
            foreach (var detail in screenData.InputDetails)
            {
                if (!string.IsNullOrEmpty(detail.InputNavigation))
                {
                    return detail.InputNavigation.Trim();
                }
            }

            throw new KeyNotFoundException($"No Input Navigation found for Screen: '{screenKey}'. Please ensure that the screen name is correct.");
        }

        /// <summary>
        /// Retrieves the verification navigation path for a given screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to get the verification navigation path for</param>
        /// <returns>The verification navigation path for the specified screen, or null if not found</returns>
        public string? GetVerificationNavigationByScreen(string screenKey)
        {
            // Check if screen data exists
            if (!result.TryGetValue(screenKey, out var screenData))
                throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

            // Find the first verification detail with navigation - optimized lookup
            foreach (var detail in screenData.VerificationDetails)
            {
                if (!string.IsNullOrEmpty(detail.VerificationNavigation))
                {
                    return detail.VerificationNavigation.Trim();
                }
            }
            throw new KeyNotFoundException($"No Verification Navigation found for Screen: '{screenKey}'. Please ensure that the screen name is correct.");
        }

        /// <summary>
        /// Gets the total number of worksheets in the Excel file.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <returns>Number of worksheets in the Excel file</returns>
        public int GetWorksheetCount(string filePath)
        {
            try
            {
                // Validate that the file path is provided and not empty
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    int worksheetCount = package.Workbook.Worksheets.Count;
                    Utilities.Logger.Log(LogType.Info, $"Excel file '{filePath}' contains {worksheetCount} worksheet(s)");
                    return worksheetCount;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetWorksheetCount failed for filePath: {filePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the names of all worksheets in the Excel file.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <returns>List of worksheet names</returns>
        public List<string> GetWorksheetNames(string filePath)
        {
            try
            {
                // Validate that the file path is provided and not empty
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheetNames = package.Workbook.Worksheets.Select(w => w.Name).ToList();
                    Utilities.Logger.Log(LogType.Info, $"Excel file '{filePath}' contains worksheets: {string.Join(", ", worksheetNames)}");
                    return worksheetNames;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetWorksheetNames failed for filePath: {filePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the row count for Input fields that start with "Row" for a specific screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to get Input field row count for</param>
        /// <returns>Number of grouped rows based on Input field names that start with "Row"</returns>
        public int GetEntriesForInputFields(string screenKey)
        {
            try
            {
                // Check if screen data exists
                if (!result.TryGetValue(screenKey, out var screenData))
                    throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

                // Get all input fields for the screen, filtering out null or empty values
                var inputFields = screenData.InputDetails
                    .Where(detail => !string.IsNullOrEmpty(detail.InputField))
                    .Select(detail => detail.InputField?.Trim())
                    .Distinct()
                    .ToList();

                if (inputFields.Count == 0)
                {
                    throw new InvalidOperationException($"No input fields found for Screen: '{screenKey}'.");
                }

                var groupedRows = inputFields
                    .Where(k => k != null && k.StartsWith("Row"))
                    .GroupBy(k => k!.Split('_')[0]) // Use null-forgiving operator (!) since nulls are filtered out
                    .ToDictionary(g => g.Key, g => g.ToList());

                Utilities.Logger.Log(LogType.Info, $"{screenKey} screen has {groupedRows.Count} row(s) of data to enter");
                return groupedRows.Count;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAllInputFieldsByScreen failed for screenKey: {screenKey}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets the row count for verification fields that start with "Row" for a specific screen.
        /// </summary>
        /// <param name="screenKey">The screen name/key to get verification field row count for</param>
        /// <returns>Number of grouped rows based on verification field names that start with "Row"</returns>
        public int GetEntriesForVerificationFields(string screenKey)
        {
            try
            {
                // Check if screen data exists
                if (!result.TryGetValue(screenKey, out var screenData))
                    throw new KeyNotFoundException($"No data found for Screen: '{screenKey}'. Please ensure that LoadExcelAndMapData is called and the screen name is correct.");

                // Get all verification fields for the screen, filtering out null or empty values
                var verificationFields = screenData.VerificationDetails
                    .Where(detail => !string.IsNullOrEmpty(detail.VerificationField))
                    .Select(detail => detail.VerificationField?.Trim())
                    .Distinct()
                    .ToList();

                if (verificationFields.Count == 0)
                {
                    throw new InvalidOperationException($"No verification fields found for Screen: '{screenKey}'.");
                }

                var groupedRows = verificationFields
                    .Where(k => k != null && k.StartsWith("Row")) // Add null check before calling StartsWith
                    .GroupBy(k => k!.Split('_')[0]) // Use null-forgiving operator (!) since nulls are filtered out
                    .ToDictionary(g => g.Key, g => g.ToList());
                Utilities.Logger.Log(LogType.Info, $"{screenKey} screen has {groupedRows.Count} row(s) of data to enter.");

                return groupedRows.Count;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetRowCountForVerificationFields failed for screenKey: {screenKey}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Verifies that the specified column headers are present in the Excel worksheet.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="expectedHeaders">List of column headers to verify</param>
        /// <param name="worksheetName">Optional name of the specific worksheet to verify. If empty, uses the first worksheet</param>
        /// <returns>True if all headers are present, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when file path is null/empty or expected headers list is null/empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the Excel file doesn't exist</exception>
        public bool VerifyColumnHeaders(string filePath, List<string> expectedHeaders, string worksheetName = "")
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                if (expectedHeaders == null || expectedHeaders.Count == 0)
                {
                    throw new ArgumentException("Expected headers list cannot be null or empty.", nameof(expectedHeaders));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    // If worksheetName is provided, find the worksheet by name
                    if (!string.IsNullOrEmpty(worksheetName))
                    {
                        worksheet = package.Workbook.Worksheets[worksheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        }
                    }
                    else
                    {
                        // Default to first worksheet if no name is provided
                        worksheet = package.Workbook.Worksheets[0];
                    }

                    if (worksheet.Dimension == null)
                    {
                        Utilities.Logger.Log(LogType.Warning, $"Worksheet '{worksheet.Name}' is empty or has no data");
                        return false;
                    }

                    int colCount = worksheet.Dimension.End.Column;

                    // Read actual headers from the Excel file (case-insensitive)
                    var actualHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            actualHeaders.Add(headerValue);
                        }
                    }

                    // Log discovered headers for debugging
                    Utilities.Logger.Log(LogType.Info, $"Headers found in worksheet '{worksheet.Name}': {string.Join(", ", actualHeaders)}");

                    // Check if all expected headers are present
                    var missingHeaders = expectedHeaders.Where(header => !actualHeaders.Contains(header)).ToList();
                    var extraHeaders = actualHeaders.Where(header => !expectedHeaders.Contains(header, StringComparer.OrdinalIgnoreCase)).ToList();

                    if (missingHeaders.Any())
                    {
                        Utilities.Logger.Log(LogType.Error, $"Missing required headers: {string.Join(", ", missingHeaders)}");
                        return false;
                    }

                    if (extraHeaders.Any())
                    {
                        Utilities.Logger.Log(LogType.Warning, $"Extra headers found (not in expected list): {string.Join(", ", extraHeaders)}");
                    }

                    Utilities.Logger.Log(LogType.Info, $"All expected headers verified successfully in worksheet '{worksheet.Name}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nVerifyColumnHeaders failed for filePath: {filePath} and worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if a specific item/value is present in a given column of the Excel worksheet.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="columnName">Name of the column to search in</param>
        /// <param name="searchValue">Value to search for in the column</param>
        /// <param name="worksheetName">Optional name of the specific worksheet to search. If empty, uses the first worksheet</param>
        /// <returns>True if the item is found, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when file path, column name, or search value is null/empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the Excel file doesn't exist</exception>
        public bool IsItemPresentInColumn(string filePath, string columnName, string searchValue, string worksheetName = "")
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                if (string.IsNullOrEmpty(columnName))
                {
                    throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
                }

                if (string.IsNullOrEmpty(searchValue))
                {
                    throw new ArgumentException("Search value cannot be null or empty.", nameof(searchValue));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    // If worksheetName is provided, find the worksheet by name
                    if (!string.IsNullOrEmpty(worksheetName))
                    {
                        worksheet = package.Workbook.Worksheets[worksheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        }
                    }
                    else
                    {
                        // Default to first worksheet if no name is provided
                        worksheet = package.Workbook.Worksheets[0];
                    }

                    if (worksheet.Dimension == null)
                    {
                        Utilities.Logger.Log(LogType.Warning, $"Worksheet '{worksheet.Name}' is empty or has no data");
                        return false;
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;

                    // Find the column index by name (case-insensitive for column headers)
                    int targetColumnIndex = -1;
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerValue) &&
                            string.Equals(headerValue, columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetColumnIndex = col;
                            break;
                        }
                    }

                    if (targetColumnIndex == -1)
                    {
                        var availableColumns = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Text?.Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                availableColumns.Add(headerValue);
                            }
                        }
                        throw new ArgumentException($"Column '{columnName}' not found in worksheet '{worksheet.Name}'. Available columns: {string.Join(", ", availableColumns)}");
                    }

                    // Search for the value in the specified column (case-insensitive, exact match)
                    for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip headers)
                    {
                        var cellValue = worksheet.Cells[row, targetColumnIndex].Text?.Trim();
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            if (string.Equals(cellValue, searchValue, StringComparison.OrdinalIgnoreCase))
                            {
                                Utilities.Logger.Log(LogType.Info, $"Item '{searchValue}' found in column '{columnName}' at row {row} in worksheet '{worksheet.Name}'");
                                return true;
                            }
                        }
                    }

                    Utilities.Logger.Log(LogType.Info, $"Item '{searchValue}' not found in column '{columnName}' in worksheet '{worksheet.Name}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nIsItemPresentInColumn failed for filePath: {filePath}, columnName: {columnName}, searchValue: {searchValue}, worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Checks if a specific worksheet exists in the Excel file.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="worksheetName">Name of the worksheet to check for</param>
        /// <returns>True if the worksheet exists, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when file path or worksheet name is null/empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the Excel file doesn't exist</exception>
        public bool IsWorksheetPresent(string filePath, string worksheetName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                if (string.IsNullOrEmpty(worksheetName))
                {
                    throw new ArgumentException("Worksheet name cannot be null or empty.", nameof(worksheetName));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Check if the worksheet exists (case-insensitive comparison)
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(w =>
                        string.Equals(w.Name, worksheetName, StringComparison.OrdinalIgnoreCase));

                    bool exists = worksheet != null;

                    if (exists)
                    {
                        Utilities.Logger.Log(LogType.Info, $"Worksheet '{worksheetName}' found in Excel file '{filePath}'");
                    }
                    else
                    {
                        var availableWorksheets = package.Workbook.Worksheets.Select(w => w.Name).ToList();
                        var worksheetList = availableWorksheets.Count > 0
                            ? string.Join(", ", availableWorksheets)
                            : "No worksheets found";

                        Utilities.Logger.Log(LogType.Info, $"Worksheet '{worksheetName}' not found in Excel file '{filePath}'. Available worksheets: {worksheetList}");
                    }

                    return exists;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nIsWorksheetPresent failed for filePath: {filePath} and worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Gets all column values for a specific client identifier from the Excel worksheet by searching in a specified column.
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="clientIdentifier">Client identifier value to search for</param>
        /// <param name="searchColumnName">Name of the column to search in (e.g., "Client Code", "Client", etc.)</param>
        /// <param name="worksheetName">Optional name of the specific worksheet to search. If empty, uses the first worksheet</param>
        /// <returns>Dictionary containing column names as keys and their corresponding values for the specified client</returns>
        /// <exception cref="ArgumentException">Thrown when file path, client identifier, or search column name is null/empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the Excel file doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not found or when the specified search column doesn't exist</exception>
        public Dictionary<string, string> GetAllColumnValuesByClient(string filePath, string clientIdentifier, string searchColumnName, string worksheetName = "")
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                if (string.IsNullOrEmpty(clientIdentifier))
                {
                    throw new ArgumentException("Client identifier cannot be null or empty.", nameof(clientIdentifier));
                }

                if (string.IsNullOrEmpty(searchColumnName))
                {
                    throw new ArgumentException("Search column name cannot be null or empty.", nameof(searchColumnName));
                }

                // Check if the Excel file exists
                if (!Utilities.Files.VerifyFilePresent(filePath))
                {
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    // If worksheetName is provided, find the worksheet by name
                    if (!string.IsNullOrEmpty(worksheetName))
                    {
                        worksheet = package.Workbook.Worksheets[worksheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                        }
                    }
                    else
                    {
                        // Default to first worksheet if no name is provided
                        worksheet = package.Workbook.Worksheets[0];
                    }

                    if (worksheet.Dimension == null)
                    {
                        throw new InvalidOperationException($"Worksheet '{worksheet.Name}' is empty or has no data");
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;

                    // Create a dictionary to map column names to their indices
                    var columnMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    // Read header row (row 1) to build column mapping
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            columnMapping[headerValue] = col;
                        }
                    }

                    // Find the specified search column
                    if (!columnMapping.TryGetValue(searchColumnName, out int searchColumnIndex))
                    {
                        var availableColumns = string.Join(", ", columnMapping.Keys);
                        throw new InvalidOperationException($"Search column '{searchColumnName}' not found in worksheet '{worksheet.Name}'. Available columns: {availableColumns}");
                    }

                    // Search for the client identifier in the specified column
                    int targetRowIndex = -1;
                    for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip headers)
                    {
                        var cellValue = worksheet.Cells[row, searchColumnIndex].Text?.Trim();
                        if (!string.IsNullOrEmpty(cellValue) &&
                            string.Equals(cellValue, clientIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            targetRowIndex = row;
                            break;
                        }
                    }

                    if (targetRowIndex == -1)
                    {
                        // Get available values from the search column for error message
                        var availableValues = new List<string>();
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var value = worksheet.Cells[row, searchColumnIndex].Text?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                availableValues.Add(value);
                            }
                        }

                        var valueList = availableValues.Count > 0
                            ? string.Join(", ", availableValues.Distinct().Take(10)) + (availableValues.Distinct().Count() > 10 ? "..." : "")
                            : "No values found";

                        throw new InvalidOperationException($"Client identifier '{clientIdentifier}' not found in column '{searchColumnName}' of worksheet '{worksheet.Name}'. Available values: {valueList}");
                    }

                    // Extract all column values for the found client row
                    var columnValues = new Dictionary<string, string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            var cellValue = worksheet.Cells[targetRowIndex, col].Text?.Trim() ?? string.Empty;
                            columnValues[headerValue] = cellValue;
                        }
                    }

                    Utilities.Logger.Log(LogType.Info, $"Successfully retrieved {columnValues.Count} column values for client identifier '{clientIdentifier}' from column '{searchColumnName}' in worksheet '{worksheet.Name}' at row {targetRowIndex}");

                    return columnValues;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAllColumnValuesByClient failed for filePath: {filePath}, clientIdentifier: {clientIdentifier}, searchColumnName: {searchColumnName}, worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Loads an Excel worksheet and builds a dictionary mapping column headers to their indices for subsequent data access operations.
        /// </summary>
        /// <param name="filePath">Path to the Excel file to load</param>
        /// <param name="worksheetName">Optional name of the specific worksheet to load. If empty, loads the first worksheet</param>
        public void LoadExcelAndHeaders(string filePath, string worksheetName = "")
        {
            try
            {
                if (!Utilities.Files.VerifyFilePresent(filePath))
                    throw new FileNotFoundException($"The specified Excel file '{filePath}' does not exist or is not accessible.");

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var package = new ExcelPackage(new FileInfo(filePath));

                // If worksheetName is provided, find the worksheet by name
                if (!string.IsNullOrEmpty(worksheetName))
                {
                    ExcelWorksheet = package.Workbook.Worksheets[worksheetName];
                    if (ExcelWorksheet == null)
                    {
                        throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(w => w.Name))}");
                    }
                }
                else
                {
                    // Default to first worksheet if no name is provided
                    ExcelWorksheet = package.Workbook.Worksheets[0];
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nLoadExcelAndHeaders failed for filePath: {filePath} and worksheetName: {worksheetName}\n{ex.StackTrace}", ex);
            }

            headers = new Dictionary<string, int>();

            for (int col = 1; col <= ExcelWorksheet.Dimension.Columns; col++)
            {
                var headerName = ExcelWorksheet.Cells[1, col].Text.Trim();

                if (!string.IsNullOrEmpty(headerName) && !headers.ContainsKey(headerName))
                {
                    headers.Add(headerName, col);
                }
            }
        }

        /// <summary>
        /// Returns the total number of data rows in the loaded Excel worksheet, excluding the header row.
        /// </summary>
        /// <returns>Count of data rows (total rows minus 1 for header)</returns>
        public int GetRowCount()
        {
            try
            {
                if (ExcelWorksheet == null)
                    throw new InvalidOperationException("Excel worksheet is not loaded. Please call LoadExcelAndHeaders first.");
                return ExcelWorksheet.Dimension.Rows - 1;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetRowCount failed\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific field value for a given scenario from the loaded Excel worksheet by matching the scenario name in the "Scenarios" column.
        /// </summary>
        /// <param name="scenarioName">The scenario name to search for in the "Scenarios" column</param>
        /// <param name="columnName">The column name from which to retrieve the value</param>
        /// <returns>The field value found for the specified scenario and column</returns>
        public string GetFieldValue(string scenarioName, string columnName)
        {
            try
            {
                if (ExcelWorksheet == null)
                    throw new InvalidOperationException("Excel worksheet is not loaded. Please call LoadExcelAndHeaders first.");

                if (!headers.ContainsKey(columnName))
                    throw new ArgumentException($"Column '{columnName}' not found.");

                int scenarioCol = headers["Scenarios"];
                int targetCol = headers[columnName];

                for (int row = 2; row <= ExcelWorksheet?.Dimension.Rows; row++)
                {
                    var scenarioCell = ExcelWorksheet.Cells[row, scenarioCol].Text.Trim();

                    if (scenarioCell.Equals(scenarioName, StringComparison.OrdinalIgnoreCase))
                    {
                        return ExcelWorksheet.Cells[row, targetCol].Text;
                    }
                }
                throw new ArgumentException($"Scenario '{scenarioName}' not found.");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetValue failed for scenarioName: {scenarioName} and columnName: {columnName}\n{ex.StackTrace}", ex);
            }
        }

    }
}
