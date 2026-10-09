using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Azure DevOps integration utility for work item management, test case operations, and attachment handling
    /// </summary>
    public class Azure
    {
        #region Private Fields
        private static string? AzureURL;
        private static string? AzureOrganization;
        private static string? AzureProject;
        private static string? PersonalAccessToken;
        private static TestCaseDetails? TestCaseInfo;
        #endregion

        #region Data Models

        /// <summary>
        /// Azure DevOps API response container for work item queries
        /// </summary>
        public class WorkItemResponse
        {
            public int? count { get; set; }
            public WorkItemValue[]? value { get; set; }
        }

        /// <summary>
        /// Complete work item structure with fields, relations, and metadata
        /// </summary>
        public class WorkItemValue
        {
            public int? id { get; set; }
            public int? rev { get; set; }
            public WorkItemFields? fields { get; set; }
            public WorkItemRelation[]? relations { get; set; }
            public WorkItemLinks? _links { get; set; }
            public string? url { get; set; }
        }

        /// <summary>
        /// Work item field collection including system and custom properties
        /// </summary>
        public class WorkItemFields
        {
            [JsonProperty("System.Id")]
            public int? SystemId { get; set; }

            [JsonProperty("System.Title")]
            public string? SystemTitle { get; set; }

            [JsonProperty("System.WorkItemType")]
            public string? SystemWorkItemType { get; set; }

            [JsonProperty("System.State")]
            public string? SystemState { get; set; }

            [JsonProperty("System.AssignedTo")]
            public object? SystemAssignedTo { get; set; }

            [JsonProperty("System.CreatedBy")]
            public object? SystemCreatedBy { get; set; }

            [JsonProperty("System.CreatedDate")]
            public DateTime? SystemCreatedDate { get; set; }

            [JsonProperty("System.ChangedBy")]
            public object? SystemChangedBy { get; set; }

            [JsonProperty("System.ChangedDate")]
            public DateTime? SystemChangedDate { get; set; }

            [JsonProperty("System.AreaPath")]
            public string? SystemAreaPath { get; set; }

            [JsonProperty("System.IterationPath")]
            public string? SystemIterationPath { get; set; }

            [JsonProperty("System.Description")]
            public string? SystemDescription { get; set; }

            [JsonProperty("Microsoft.VSTS.Common.Priority")]
            public int? Priority { get; set; }

            [JsonProperty("Microsoft.VSTS.Common.Severity")]
            public string? Severity { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.Steps")]
            public string? TestCaseSteps { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.Parameters")]
            public string? TestCaseParameters { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.LocalDataSource")]
            public string? TestCaseLocalDataSource { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestName")]
            public string? AutomatedTestName { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.AutomatedTestStorage")]
            public string? AutomatedTestStorage { get; set; }

            [JsonProperty("Microsoft.VSTS.TCM.AutomationStatus")]
            public string? AutomationStatus { get; set; }
        }

        /// <summary>
        /// Work item relationship definitions for attachments, links, and dependencies
        /// </summary>
        public class WorkItemRelation
        {
            public string? rel { get; set; }
            public string? url { get; set; }
            public WorkItemRelationAttributes? attributes { get; set; }
        }

        /// <summary>
        /// Relationship metadata including timestamps, size, and descriptive attributes
        /// </summary>
        public class WorkItemRelationAttributes
        {
            public DateTime? authorizedDate { get; set; }
            public int? id { get; set; }
            public DateTime? resourceCreatedDate { get; set; }
            public DateTime? resourceModifiedDate { get; set; }
            public DateTime? revisedDate { get; set; }
            public int? resourceSize { get; set; }
            public string? name { get; set; }
            public string? comment { get; set; }
        }

        /// <summary>
        /// Work item navigation links for API endpoints and web interfaces
        /// </summary>
        public class WorkItemLinks
        {
            public WorkItemLink? self { get; set; }
            public WorkItemLink? workItemUpdates { get; set; }
            public WorkItemLink? workItemRevisions { get; set; }
            public WorkItemLink? workItemHistory { get; set; }
            public WorkItemLink? html { get; set; }
            public WorkItemLink? workItemType { get; set; }
            public WorkItemLink? fields { get; set; }
        }

        /// <summary>
        /// Individual navigation link with URL reference
        /// </summary>
        public class WorkItemLink
        {
            public string? href { get; set; }
        }

        /// <summary>
        /// Complete test case information with steps, parameters, and attachments
        /// </summary>
        public class TestCaseDetails
        {
            public string? Id { get; set; }
            public string? Title { get; set; }
            public string? WorkItemType { get; set; }
            public string? State { get; set; }
            public string? AssignedTo { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime? CreatedDate { get; set; }
            public string? ChangedBy { get; set; }
            public DateTime? ChangedDate { get; set; }
            public string? AreaPath { get; set; }
            public string? IterationPath { get; set; }
            public string? Description { get; set; }
            public int? Priority { get; set; }
            public string? Severity { get; set; }
            public string? AutomatedTestName { get; set; }
            public string? AutomatedTestStorage { get; set; }
            public string? AutomationStatus { get; set; }
            public List<TestStep>? Steps { get; set; }
            public List<Dictionary<string, string>>? Parameters { get; set; }
            public List<WorkItemAttachment>? Attachments { get; set; }
        }

        /// <summary>
        /// Test step definition with action, expected result, and shared step reference
        /// </summary>
        public class TestStep
        {
            public int StepNumber { get; set; }
            public string? Action { get; set; }
            public string? ExpectedResult { get; set; }
            public string? SharedStepId { get; set; }
        }

        /// <summary>
        /// Work item attachment details with download URL and metadata
        /// </summary>
        public class WorkItemAttachment
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Url { get; set; }
            public DateTime? CreatedDate { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public int? Size { get; set; }
            public string? Comment { get; set; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Configures Azure DevOps connection parameters for API authentication
        /// </summary>
        /// <param name="organizationName">Azure DevOps organization name (e.g., 'yourorganization' for https://dev.azure.com/yourorganization)</param>
        /// <param name="projectName">Name of the Azure DevOps project</param>
        /// <param name="personalAccessToken">Personal Access Token for authentication</param>
        public void Initialize(string azureURL, string organizationName, string projectName, string personalAccessToken)
        {
            if (string.IsNullOrWhiteSpace(azureURL))
                throw new ArgumentException("Azure URL cannot be null or empty", nameof(azureURL));

            if (string.IsNullOrWhiteSpace(organizationName))
                throw new ArgumentException("Organization name cannot be null or empty", nameof(organizationName));

            if (string.IsNullOrWhiteSpace(projectName))
                throw new ArgumentException("Project name cannot be null or empty", nameof(projectName));

            if (string.IsNullOrWhiteSpace(personalAccessToken))
                throw new ArgumentException("Personal access token cannot be null or empty", nameof(personalAccessToken));

            AzureURL = azureURL;
            AzureOrganization = organizationName;
            AzureProject = projectName;
            PersonalAccessToken = personalAccessToken;
        }

        /// <summary>
        /// Ensures Azure DevOps connection has been properly initialized
        /// </summary>
        private void ValidateInitialization()
        {
            if (string.IsNullOrEmpty(AzureURL) || string.IsNullOrEmpty(AzureOrganization) || string.IsNullOrEmpty(AzureProject) || string.IsNullOrEmpty(PersonalAccessToken))
                throw new InvalidOperationException("Azure connection not initialized. Call Initialize method first.");
        }
        #endregion

        #region Core Work Item Methods

        /// <summary>
        /// Fetches complete work item data including all fields and relations
        /// </summary>
        /// <param name="workItemId">The work item ID to retrieve</param>
        /// <returns>Complete work item data or null if not found</returns>
        public WorkItemValue? GetWorkItemDetails(string workItemId)
        {
            ValidateInitialization();
            if (string.IsNullOrWhiteSpace(workItemId))
                throw new ArgumentException("Work item ID cannot be null or empty", nameof(workItemId));

            try
            {
                string apiUrl = $"{AzureURL}/{AzureOrganization}/{AzureProject}/_apis/wit/workitems?ids={workItemId}&$expand=all&api-version=7.0";

                using var client = CreateHttpClient();
                using var response = client.GetAsync(apiUrl).Result;

                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var workItemResponse = JsonConvert.DeserializeObject<WorkItemResponse>(responseBody);
                var workItem = workItemResponse?.value?.FirstOrDefault();

                return workItem;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception($"{ex.Message}\nAzure DevOps request unauthorized. Verify the configured Personal Access Token (VSTSPAT).\n{ex.StackTrace}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetWorkItemDetails failed for workitemID: {workItemId}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Retrieves multiple work items in a single API call for efficient batch processing
        /// </summary>
        /// <param name="workItemIds">Array of work item IDs to retrieve</param>
        /// <returns>Array of work item details</returns>
        public WorkItemValue[]? GetMultipleWorkItems(string[] workItemIds)
        {
            ValidateInitialization();

            if (workItemIds == null || workItemIds.Length == 0)
                throw new ArgumentException("Work item IDs array cannot be null or empty", nameof(workItemIds));

            try
            {
                string ids = string.Join(",", workItemIds);
                string apiUrl = $"{AzureURL}/{AzureOrganization}/{AzureProject}/_apis/wit/workitems?ids={ids}&$expand=all&api-version=7.0";

                using var client = CreateHttpClient();
                using var response = client.GetAsync(apiUrl).Result;

                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var workItemResponse = JsonConvert.DeserializeObject<WorkItemResponse>(responseBody);
                var workItems = workItemResponse?.value;

                return workItems;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetMultipleWorkItems failed for workitemIDs: {string.Join(",", workItemIds)}\n{ex.StackTrace}", ex);
            }
        }

        #endregion

        #region Test Case Specific Methods

        /// <summary>
        /// Extracts and parses complete test case information from work item data
        /// </summary>
        /// <param name="testCaseId">The test case work item ID</param>
        /// <returns>Complete test case details or null if not found</returns>
        public void LoadTestCaseInfo(string testCaseId)
        {
            var workItem = GetWorkItemDetails(testCaseId);
            if (workItem?.fields == null)
            {
                throw new Exception($"Work item with ID {testCaseId} not found or has no fields.");
            }

            try
            {
                var testCaseDetails = new TestCaseDetails
                {
                    Id = workItem.id?.ToString(),
                    Title = workItem.fields.SystemTitle,
                    WorkItemType = workItem.fields.SystemWorkItemType,
                    State = workItem.fields.SystemState,
                    AssignedTo = ExtractDisplayName(workItem.fields.SystemAssignedTo),
                    CreatedBy = ExtractDisplayName(workItem.fields.SystemCreatedBy),
                    CreatedDate = workItem.fields.SystemCreatedDate,
                    ChangedBy = ExtractDisplayName(workItem.fields.SystemChangedBy),
                    ChangedDate = workItem.fields.SystemChangedDate,
                    AreaPath = workItem.fields.SystemAreaPath,
                    IterationPath = workItem.fields.SystemIterationPath,
                    Description = workItem.fields.SystemDescription,
                    Priority = workItem.fields.Priority,
                    Severity = workItem.fields.Severity,
                    AutomatedTestName = workItem.fields.AutomatedTestName,
                    AutomatedTestStorage = workItem.fields.AutomatedTestStorage,
                    AutomationStatus = workItem.fields.AutomatedTestStorage,
                    Steps = ParseTestCaseSteps(workItem.fields.TestCaseSteps),
                    Parameters = ParseTestCaseParameters(testCaseId, workItem.fields.TestCaseLocalDataSource),
                    Attachments = ParseWorkItemAttachments(workItem.relations)
                };
                TestCaseInfo = testCaseDetails;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetTestCaseInfo failed for workitemID: {testCaseId}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Extracts test case execution steps from test case details
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing steps</param>
        /// <returns>List of test steps or throws exception if none found</returns>
        public List<TestStep>? GetTestCaseSteps()
        {
            var steps = TestCaseInfo?.Steps;
            if (steps == null)
            {
                throw new Exception($"No steps present for the WorkitemID: {TestCaseInfo?.Id}");
            }
            return steps;
        }

        /// <summary>
        /// Extracts test case parameter data from test case details
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing parameters</param>
        /// <returns>List of parameter dictionaries or throws exception if none found</returns>
        public List<Dictionary<string, string>>? GetAllParameters()
        {
            var parameters = TestCaseInfo?.Parameters;
            if (parameters == null)
            {
                throw new Exception($"No parameters present for the WorkitemID: {TestCaseInfo?.Id}");
            }
            return parameters;
        }

        /// <summary>
        /// Retrieves specific parameter value by name and row index with validation
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing parameters</param>
        /// <param name="parameterName">Name of the parameter to retrieve</param>
        /// <param name="rowIndex">1-based row index (default: 1)</param>
        /// <returns>Parameter value or null if not found</returns>
        public string? GetParameterValue(string parameterName, int rowIndex = 1)
        {
            var parameters = TestCaseInfo?.Parameters;
            if (parameters == null)
                throw new Exception($"No parameters present for the WorkitemID: {TestCaseInfo?.Id}");

            if (rowIndex < 1 || rowIndex > parameters.Count)
                throw new Exception($"No data for row {rowIndex}. Only {parameters.Count} Parameter Rows present for the WorkitemID: {TestCaseInfo?.Id}");

            var row = parameters[rowIndex - 1];

            if (!row.ContainsKey(parameterName))
                throw new KeyNotFoundException($"{parameterName} does not exist in the Parameters for the WorkitemID: {TestCaseInfo?.Id}");

            return row[parameterName];
        }

        /// <summary>
        /// Retrieves all parameter key-value pairs for a specific row index with validation
        /// </summary>
        /// <param name="rowIndex">1-based row index (default: 1)</param>
        /// <returns>Dictionary containing all parameter names and values for the specified row</returns>
        public Dictionary<string, string> GetAllParametersForIndex(int rowIndex = 1)
        {
            var parameters = TestCaseInfo?.Parameters;
            if (parameters == null)
                throw new Exception($"No parameters present for the WorkitemID: {TestCaseInfo?.Id}");

            if (rowIndex < 1 || rowIndex > parameters.Count)
                throw new Exception($"No data for row {rowIndex}. Only {parameters.Count} Parameter Rows present for the WorkitemID: {TestCaseInfo?.Id}");

            Dictionary<string, string> row = parameters[rowIndex - 1];

            // Return a new dictionary to avoid external modification of internal data
            return row;
        }

        /// <summary>
        /// Finds row index by matching parameter value with case-insensitive comparison       
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing parameters</param>
        /// <param name="parameterName">Name of the parameter column to search in</param>
        /// <param name="columnValue">Value to search for in the specified parameter column</param>
        /// <returns>1-based row number if found; throws KeyNotFoundException if not found</returns>       
        public int GetRowIndexForParameter(string parameterName, string columnValue)
        {
            if (TestCaseInfo?.Parameters == null)
                throw new Exception($"No parameters present for the WorkitemID: {TestCaseInfo?.Id}");

            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));

            if (string.IsNullOrWhiteSpace(columnValue))
                throw new ArgumentException("Column value cannot be null or empty", nameof(columnValue));

            for (int i = 0; i < TestCaseInfo.Parameters.Count; i++)
            {
                var row = TestCaseInfo.Parameters[i];

                if (row.ContainsKey(parameterName))
                {
                    var cellValue = row[parameterName];

                    // Use case-insensitive comparison and trim whitespace
                    if (string.Equals(cellValue?.Trim(), columnValue.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return i + 1; // Return 1-based row number
                    }
                }
            }

            // If not found, provide helpful error information
            var availableValues = new List<string>();
            for (int i = 0; i < TestCaseInfo.Parameters.Count; i++)
            {
                var row = TestCaseInfo.Parameters[i];
                if (row.ContainsKey(parameterName) && !string.IsNullOrEmpty(row[parameterName]))
                {
                    availableValues.Add($"Row {i + 1}: '{row[parameterName]}'");
                }
            }

            var availableValuesText = availableValues.Count > 0
                ? string.Join(", ", availableValues.Take(10)) + (availableValues.Count > 10 ? "..." : "")
                : "No values found";

            throw new KeyNotFoundException($"Value '{columnValue}' not found in parameter '{parameterName}' for WorkitemID: {TestCaseInfo.Id}. Available values: {availableValuesText}");
        }

        #endregion

        #region Attachment Methods

        /// <summary>
        /// Extracts work item attachments from test case details
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing attachments</param>
        /// <returns>List of attachments or throws exception if none found</returns>
        public List<WorkItemAttachment>? GetWorkItemAttachments()
        {
            var attachments = TestCaseInfo?.Attachments;
            if (attachments == null)
            {
                throw new Exception($"No attachments present for the WorkitemID: {TestCaseInfo?.Id}");
            }
            return attachments;
        }

        /// <summary>
        /// Downloads all work item attachments to specified directory with progress logging
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing attachments</param>
        /// <param name="downloadPath">Local directory path to save attachments</param>
        /// <param name="overwriteExisting">Whether to overwrite existing files</param>
        /// <returns>List of downloaded file paths</returns>
        public List<string> DownloadAllAttachments(string downloadPath, bool overwriteExisting = true)
        {
            var attachments = TestCaseInfo?.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                Utilities.Logger.Log(Logger.LogType.Warning, $"No attachments to download for work item ID: {TestCaseInfo?.Id}");
                return new List<string>();
            }

            // Ensure download directory exists
            Directory.CreateDirectory(downloadPath);

            var downloadedFiles = new List<string>();

            foreach (var attachment in attachments)
            {
                if (!string.IsNullOrEmpty(attachment.Url) && !string.IsNullOrEmpty(attachment.Name))
                {
                    try
                    {
                        string filePath = Path.Combine(downloadPath, attachment.Name);

                        if (File.Exists(filePath) && !overwriteExisting)
                        {
                            continue;
                        }

                        DownloadSingleAttachment(attachment.Url, filePath);
                        downloadedFiles.Add(filePath);
                    }
                    catch (Exception ex)
                    {
                        Utilities.Logger.Log(Logger.LogType.Error, $"Failed to download attachment '{attachment.Name}': {ex.Message}");
                        throw new Exception($"{ex.Message}\nDownloadAllAttachments failed downloading attachment {attachment.Name} for workitemID: {TestCaseInfo?.Id}\n{ex.StackTrace}", ex);
                    }
                }
            }

            Utilities.Logger.Log(Logger.LogType.Pass, $"Downloaded {downloadedFiles.Count} attachments for work item {TestCaseInfo?.Id}");
            return downloadedFiles;
        }

        /// <summary>
        /// Downloads specific attachment by name with optional overwrite control
        /// </summary>
        /// <param name="testCaseDetails">The test case details containing attachments</param>
        /// <param name="attachmentName">Name of the attachment to download</param>
        /// <param name="downloadPath">Local directory path to save the attachment</param>
        /// <param name="overwriteExisting">Whether to overwrite existing file</param>
        /// <returns>Downloaded file path or null if attachment not found</returns>
        public string? DownloadSpecificAttachment(string attachmentName, string downloadPath, bool overwriteExisting = true)
        {
            var attachments = TestCaseInfo?.Attachments;
            if (attachments == null)
            {
                Utilities.Logger.Log(Logger.LogType.Warning, $"No attachments found for work item ID: {TestCaseInfo?.Id}");
                return null;
            }

            var targetAttachment = attachments.FirstOrDefault(a =>
                string.Equals(a.Name, attachmentName, StringComparison.OrdinalIgnoreCase));

            if (targetAttachment?.Url == null)
            {
                Utilities.Logger.Log(Logger.LogType.Warning, $"Attachment '{attachmentName}' not found for work item ID: {TestCaseInfo?.Id}");
                return null;
            }

            Directory.CreateDirectory(downloadPath);
            string filePath = Path.Combine(downloadPath, targetAttachment.Name ?? attachmentName);

            if (File.Exists(filePath) && !overwriteExisting)
            {
                Utilities.Logger.Log(Logger.LogType.Skip, $"File already exists: {filePath}");
                return filePath;
            }

            try
            {
                DownloadSingleAttachment(targetAttachment.Url, filePath);
                Utilities.Logger.Log(Logger.LogType.Pass, $"Successfully downloaded attachment '{attachmentName}' to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(Logger.LogType.Error, $"Failed to download attachment '{attachmentName}': {ex.Message}");
                throw new Exception($"{ex.Message}\nDownloadSpecificAttachment failed for workitemID: {TestCaseInfo?.Id}, attachmentName: {attachmentName}\n{ex.StackTrace}", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates HTTP client with Azure DevOps authentication and security configuration
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{PersonalAccessToken}")));

            // Set security protocols
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            return client;
        }

        /// <summary>
        /// Extracts display name from Azure DevOps user object with fallback handling
        /// </summary>
        private string? ExtractDisplayName(object? userObject)
        {
            if (userObject == null)
                return null;

            try
            {
                if (userObject is string stringValue)
                    return stringValue;

                var userJson = JsonConvert.SerializeObject(userObject);
                var userDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(userJson);

                return userDict?.GetValueOrDefault("displayName")?.ToString() ??
                       userDict?.GetValueOrDefault("uniqueName")?.ToString();
            }
            catch
            {
                return userObject.ToString();
            }
        }

        /// <summary>
        /// Parses XML test case steps with fallback to text parsing for robustness
        /// </summary>
        private List<TestStep>? ParseTestCaseSteps(string? stepsXml)
        {
            if (string.IsNullOrEmpty(stepsXml))
                return null;

            try
            {
                var steps = new List<TestStep>();
                var doc = new XmlDocument();
                doc.LoadXml(stepsXml);

                var stepNodes = doc.SelectNodes("//step");
                if (stepNodes == null)
                    return null;

                foreach (XmlNode stepNode in stepNodes)
                {
                    var stepIdAttr = stepNode.Attributes?["id"];
                    if (!int.TryParse(stepIdAttr?.Value, out int stepNumber))
                        stepNumber = steps.Count + 1;

                    var paramNodes = stepNode.SelectNodes(".//parameterizedString[@isformatted='true']");

                    string? action = null;
                    string? expectedResult = null;
                    string? sharedStepId = null;

                    // Check for shared step reference
                    var comprefNode = stepNode.SelectSingleNode(".//compref");
                    if (comprefNode?.Attributes?["ref"] != null)
                    {
                        sharedStepId = comprefNode.Attributes["ref"]?.Value;
                    }

                    if (paramNodes != null && paramNodes.Count > 0)
                    {
                        action = CleanHtmlContent(paramNodes[0]?.InnerText);

                        if (paramNodes.Count > 1)
                            expectedResult = CleanHtmlContent(paramNodes[1]?.InnerText);
                    }

                    if (!string.IsNullOrWhiteSpace(action) || !string.IsNullOrEmpty(sharedStepId))
                    {
                        steps.Add(new TestStep
                        {
                            StepNumber = stepNumber,
                            Action = action,
                            ExpectedResult = expectedResult,
                            SharedStepId = sharedStepId
                        });
                    }
                }

                // Sort and renumber steps
                steps = steps.OrderBy(s => s.StepNumber).ToList();
                for (int i = 0; i < steps.Count; i++)
                {
                    steps[i].StepNumber = i + 1;
                }

                return steps.Count > 0 ? steps : null;
            }
            catch (Exception)
            {
                // Fallback to text parsing if XML parsing fails
                return ParseStepsFromText(stepsXml);
            }
        }

        /// <summary>
        /// Text-based step parser for non-XML formatted test case steps
        /// </summary>
        private List<TestStep>? ParseStepsFromText(string stepsText)
        {
            try
            {
                var steps = new List<TestStep>();
                var lines = stepsText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                int stepNumber = 1;
                string? currentAction = null;

                foreach (var line in lines)
                {
                    var cleanLine = CleanHtmlContent(line.Trim());
                    if (!string.IsNullOrEmpty(cleanLine))
                    {
                        if (currentAction == null)
                        {
                            currentAction = cleanLine;
                        }
                        else
                        {
                            steps.Add(new TestStep
                            {
                                StepNumber = stepNumber++,
                                Action = currentAction,
                                ExpectedResult = cleanLine
                            });
                            currentAction = null;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(currentAction))
                {
                    steps.Add(new TestStep
                    {
                        StepNumber = stepNumber,
                        Action = currentAction,
                        ExpectedResult = null
                    });
                }

                return steps.Count > 0 ? steps : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts XML DataSet format to structured parameter dictionaries
        /// </summary>
        /// <param name="xmlString">XML string containing DataSet with Table1 elements</param>
        /// <returns>List of dictionaries where each dictionary represents a row with column names as keys and values as objects</returns>
        private List<Dictionary<string, string>>? ParseTestCaseParameters(string testCaseId, string? xmlString)
        {
            if (string.IsNullOrWhiteSpace(xmlString))
                return null;

            var resultList = new List<Dictionary<string, string>>();

            try
            {
                var dataSet = new DataSet();

                using var xmlReader = new StringReader(xmlString);
                dataSet.ReadXml(xmlReader, XmlReadMode.Auto);

                // Check if we have tables and rows
                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0]; // Get the first table (Table1)

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDictionary = new Dictionary<string, string>();

                        foreach (DataColumn column in table.Columns)
                        {
                            var value = row[column.ColumnName];

                            // Convert to string directly
                            rowDictionary[column.ColumnName] = value?.ToString() ?? string.Empty;
                        }

                        resultList.Add(rowDictionary);
                    }
                }

                return resultList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse XML to List<Dictionary<string, string>>: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts attachment metadata from work item relations
        /// </summary>
        private List<WorkItemAttachment>? ParseWorkItemAttachments(WorkItemRelation[]? relations)
        {
            if (relations == null)
                return null;

            var attachments = new List<WorkItemAttachment>();

            foreach (var relation in relations)
            {
                if (relation.rel == "AttachedFile" && !string.IsNullOrEmpty(relation.url) && relation.attributes != null)
                {
                    attachments.Add(new WorkItemAttachment
                    {
                        Id = relation.attributes.id?.ToString(),
                        Name = relation.attributes.name,
                        Url = relation.url,
                        CreatedDate = relation.attributes.resourceCreatedDate,
                        ModifiedDate = relation.attributes.resourceModifiedDate,
                        Size = relation.attributes.resourceSize,
                        Comment = relation.attributes.comment
                    });
                }
            }

            return attachments.Count > 0 ? attachments : null;
        }

        /// <summary>
        /// Downloads attachment file from Azure DevOps with proper API versioning
        /// </summary>
        private void DownloadSingleAttachment(string attachmentUrl, string filePath)
        {
            try
            {
                string downloadUrl = $"{attachmentUrl}?api-version=7.0";

                using var client = CreateHttpClient();
                using var response = client.GetAsync(downloadUrl).Result;

                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                response.Content.CopyToAsync(fileStream).Wait();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nDownloadSingleAttachment failed from {attachmentUrl} to {filePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Removes HTML tags and decodes entities for clean text output
        /// </summary>
        private string? CleanHtmlContent(string? htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;

            try
            {
                var result = Regex.Replace(htmlContent, @"</p\s*>", Environment.NewLine, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, @"<br\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);

                // Remove all remaining HTML tags
                result = Regex.Replace(result, "<.*?>", string.Empty);

                // Decode HTML entities (e.g., &gt;)
                result = System.Net.WebUtility.HtmlDecode(result);

                // Trim extra spaces
                return result.Trim();
            }
            catch
            {
                return htmlContent.Trim();
            }
        }

        #endregion

        #region Additional Utility Methods

        /// <summary>
        /// Retrieves work item title for display or logging purposes
        /// </summary>
        /// <param name="workItemId">The work item ID</param>
        /// <returns>Work item title or null if not found</returns>
        public string? GetWorkItemTitle(string workItemId)
        {
            var workItem = GetWorkItemDetails(workItemId);
            var title = workItem?.fields?.SystemTitle;
            return title;
        }

        /// <summary>
        /// Retrieves current work item state for workflow tracking
        /// </summary>
        /// <param name="workItemId">The work item ID</param>
        /// <returns>Work item state or null if not found</returns>
        public string? GetWorkItemState(string workItemId)
        {
            var workItem = GetWorkItemDetails(workItemId);
            var state = workItem?.fields?.SystemState;
            return state;
        }

        /// <summary>
        /// Retrieves work item type for filtering and categorization
        /// </summary>
        /// <param name="workItemId">The work item ID</param>
        /// <returns>Work item type or null if not found</returns>
        public string? GetWorkItemType(string workItemId)
        {
            var workItem = GetWorkItemDetails(workItemId);
            var type = workItem?.fields?.SystemWorkItemType;
            return type;
        }

        /// <summary>
        /// Validates work item existence without retrieving full details
        /// </summary>
        /// <param name="workItemId">The work item ID to check</param>
        /// <returns>True if work item exists, false otherwise</returns>
        public bool WorkItemExists(string workItemId)
        {
            try
            {
                var workItem = GetWorkItemDetails(workItemId);
                bool exists = workItem != null;
                return exists;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Data Extraction Methods

        /// <summary>
        /// Parses multi-row data with colon-separated key-value pairs and organizes them into a list of row dictionaries
        /// </summary>
        /// <param name="dataToParse">Multi-line data containing row headers (e.g., "Row 1", "Row 2") followed by colon-separated key-value pairs</param>
        /// <returns>List of dictionaries where each dictionary represents a row with its key-value pairs</returns>
        public static List<Dictionary<string, string>> ParseMultiRowData(string dataToParse)
        {
            if (string.IsNullOrWhiteSpace(dataToParse))
                throw new ArgumentException("Data to parse cannot be null or empty", nameof(dataToParse));

            var rowDataList = new List<Dictionary<string, string>>();

            try
            {
                var lines = dataToParse.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string>? currentRowData = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;

                    // Check if this line starts a new row (e.g., "Row 1", "Row 2", etc.)
                    if (trimmedLine.StartsWith("Row ", StringComparison.OrdinalIgnoreCase) &&
                        !trimmedLine.Contains(':'))
                    {
                        // Save the previous row data if it exists
                        if (currentRowData != null && currentRowData.Count > 0)
                        {
                            rowDataList.Add(currentRowData);
                        }

                        // Start a new row
                        currentRowData = new Dictionary<string, string>();
                        currentRowData["RowIdentifier"] = trimmedLine;
                    }
                    else
                    {
                        // Parse key-value pair
                        var colonIndex = trimmedLine.IndexOf(':');
                        if (colonIndex > 0 && colonIndex < trimmedLine.Length - 1)
                        {
                            var key = trimmedLine.Substring(0, colonIndex).Trim();
                            var value = trimmedLine.Substring(colonIndex + 1).Trim();

                            if (!string.IsNullOrEmpty(key))
                            {
                                // If no current row data exists, create one (for data without explicit row headers)
                                if (currentRowData == null)
                                {
                                    currentRowData = new Dictionary<string, string>();
                                }

                                currentRowData[key] = value;
                            }
                        }
                    }
                }

                // Add the last row if it exists
                if (currentRowData != null && currentRowData.Count > 0)
                {
                    rowDataList.Add(currentRowData);
                }

                return rowDataList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse multi-row data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts navigation path from test steps that contain "Navigate to"
        /// Always prioritizes finding steps with "Navigate to" and extracts the navigation path
        /// </summary>
        /// <param name="actionText">The action text to search for (e.g., "Enter Main Return Data") or navigation command (e.g., "Navigate to Tax Return Information>CT600>Main Return Data> AP End Date")</param>
        /// <returns>Navigation path string if found, or null if not found</returns>
        public string? ParseNavigation(string actionText)
        {
            if (string.IsNullOrWhiteSpace(actionText))
                throw new ArgumentException("Action text cannot be null or empty", nameof(actionText));

            try
            {
                // If TestSteps is not loaded, we can only process direct navigation commands
                if (TestCaseInfo?.Steps == null || TestCaseInfo?.Steps.Count == 0)
                    return null;

                // First, try to find a step that specifically contains "Navigate to"
                var navigationStep = GetNavigationStep(actionText);

                if (navigationStep?.Action == null)
                    return null;

                // Split the action content into lines
                var lines = navigationStep.Action.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Look for navigation paths that contain "Navigate to"
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Skip empty lines
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;

                    // Check if this line contains "Navigate to"
                    if (trimmedLine.Contains("Navigate to", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the navigation path after "Navigate to"
                        var navigateToIndex = trimmedLine.IndexOf("Navigate to", StringComparison.OrdinalIgnoreCase);
                        var navigationPath = trimmedLine.Substring(navigateToIndex + "Navigate to".Length).Trim();

                        if (!string.IsNullOrEmpty(navigationPath))
                            return navigationPath;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract navigation path for action '{actionText}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses both input and output step data for the specified screen and returns a combined
        /// list of dictionaries where each dictionary contains both input and output key/value pairs
        /// for the corresponding row. Input keys are prefixed with "Input." and output keys with "Output.".
        /// Rows are aligned by position (1-based row ordering). If one side has fewer rows, missing rows
        /// are treated as empty dictionaries for that side.
        /// </summary>
        /// <param name="screenToParse">Screen name to find in test steps</param>
        /// <returns>List of dictionaries combining input and output data per row</returns>
        public List<Dictionary<string, string>> ParseSteps(string screenToParse)
        {
            if (TestCaseInfo?.Steps == null)
                throw new Exception($"No steps present for the WorkitemID: {TestCaseInfo?.Id}");

            if (string.IsNullOrWhiteSpace(screenToParse))
                throw new ArgumentException("Screen name cannot be null or empty", nameof(screenToParse));

            // Find the first step that mentions the screen in either Action or ExpectedResult
            var matchingStep = TestCaseInfo.Steps.FirstOrDefault(step =>
                (step.Action?.Contains(screenToParse, StringComparison.OrdinalIgnoreCase) == true) ||
                (step.ExpectedResult?.Contains(screenToParse, StringComparison.OrdinalIgnoreCase) == true));

            // If no matching step found, return empty list
            if (matchingStep == null)
                return new List<Dictionary<string, string>>();

            // Local parser to convert block of lines into row dictionaries
            List<Dictionary<string, string>> ParseTextToRows(string? text)
            {
                var rows = new List<Dictionary<string, string>>();
                if (string.IsNullOrEmpty(text))
                    return rows;

                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string>? currentRow = null;

                foreach (var rawLine in lines)
                {
                    var line = CleanHtmlContent(rawLine)?.Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // Start of a new explicit row
                    if (line.StartsWith("Row ", StringComparison.OrdinalIgnoreCase) && !line.Contains(':'))
                    {
                        if (currentRow != null && currentRow.Count > 0)
                            rows.Add(currentRow);

                        currentRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["RowIdentifier"] = line
                        };
                        continue;
                    }

                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim();
                        var value = colonIndex < line.Length - 1 ? line.Substring(colonIndex + 1).Trim() : string.Empty;

                        if (!string.IsNullOrEmpty(key))
                        {
                            if (currentRow == null)
                                currentRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                            currentRow[key] = string.IsNullOrEmpty(value) ? string.Empty : value;
                        }
                    }
                }

                if (currentRow != null && currentRow.Count > 0)
                    rows.Add(currentRow);

                return rows;
            }

            var inputRows = ParseTextToRows(matchingStep.Action);
            var outputRows = ParseTextToRows(matchingStep.ExpectedResult);

            var combinedList = new List<Dictionary<string, string>>();
            int maxRows = Math.Max(inputRows.Count, outputRows.Count);

            for (int i = 0; i < maxRows; i++)
            {
                var combinedRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (i < inputRows.Count)
                {
                    foreach (var kvp in inputRows[i])
                    {
                        if (string.Equals(kvp.Key, "RowIdentifier", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!combinedRow.ContainsKey("RowIdentifier"))
                                combinedRow["RowIdentifier"] = kvp.Value ?? string.Empty;
                            continue;
                        }

                        // Use same key names as ParseInputStepsData (no "Input." prefix)
                        combinedRow[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }

                if (i < outputRows.Count)
                {
                    foreach (var kvp in outputRows[i])
                    {
                        if (string.Equals(kvp.Key, "RowIdentifier", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!combinedRow.ContainsKey("RowIdentifier"))
                                combinedRow["RowIdentifier"] = kvp.Value ?? string.Empty;
                            continue;
                        }

                        // Use same key names as ParseOutputStepsData (no "Output." prefix)
                        // If keys collide, output value will overwrite input value (output has precedence)
                        combinedRow[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }

                combinedList.Add(combinedRow);
            }

            return combinedList;
        }

        /// <summary>
        /// Parses input data from test steps matching the screen name
        /// </summary>
        /// <param name="screenToParse">Screen name to find in test steps</param>
        /// <returns>List of dictionaries where each dictionary represents a row with its key-value pairs</returns>
        public List<Dictionary<string, string>> ParseInputStepsData(string screenToParse)
        {
            if (TestCaseInfo?.Steps == null)
            {
                throw new Exception($"No steps present for the WorkitemID: {TestCaseInfo?.Id}");
            }

            if (string.IsNullOrWhiteSpace(screenToParse))
                throw new ArgumentException("Screen name cannot be null or empty", nameof(screenToParse));

            var matchingStep = GetMatchingStep(screenToParse);
            var rowDataList = new List<Dictionary<string, string>>();

            try
            {
                var lines = matchingStep?.Action?.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines == null) return rowDataList;

                Dictionary<string, string>? currentRowData = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;

                    // Check if this line starts a new row (e.g., "Row 1", "Row 2", etc.)
                    if (trimmedLine.StartsWith("Row ", StringComparison.OrdinalIgnoreCase) &&
                        !trimmedLine.Contains(':'))
                    {
                        // Save the previous row data if it exists
                        if (currentRowData != null && currentRowData.Count > 0)
                        {
                            rowDataList.Add(currentRowData);
                        }

                        // Start a new row
                        currentRowData = new Dictionary<string, string>();
                        currentRowData["RowIdentifier"] = trimmedLine;
                    }
                    else
                    {
                        // Parse key-value pair
                        var colonIndex = trimmedLine.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            var key = trimmedLine.Substring(0, colonIndex).Trim();
                            var value = colonIndex < trimmedLine.Length - 1
                                ? trimmedLine.Substring(colonIndex + 1).Trim()
                                : string.Empty;

                            if (!string.IsNullOrEmpty(key))
                            {
                                // If no current row data exists, create one (for data without explicit row headers)
                                if (currentRowData == null)
                                {
                                    currentRowData = new Dictionary<string, string>();
                                }

                                // Ensure value is never null - use empty string if null or empty
                                currentRowData[key] = string.IsNullOrEmpty(value) ? string.Empty : value;
                            }
                        }
                    }
                }

                // Add the last row if it exists
                if (currentRowData != null && currentRowData.Count > 0)
                {
                    rowDataList.Add(currentRowData);
                }
                return rowDataList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse input data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses output data from test steps matching the screen name
        /// </summary>
        /// <param name="screenToParse">Screen name to find in test steps</param>
        /// <returns>List of dictionaries where each dictionary represents a row with its key-value pairs</returns>
        public List<Dictionary<string, string>> ParseOutputStepsData(string screenToParse)
        {
            if (TestCaseInfo?.Steps == null)
            {
                throw new Exception($"No steps present for the WorkitemID: {TestCaseInfo?.Id}");
            }

            if (string.IsNullOrWhiteSpace(screenToParse))
                throw new ArgumentException("Screen name cannot be null or empty", nameof(screenToParse));

            var matchingStep = GetMatchingStep(screenToParse);
            var rowDataList = new List<Dictionary<string, string>>();

            try
            {
                var lines = matchingStep?.ExpectedResult?.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines == null) return rowDataList;

                Dictionary<string, string>? currentRowData = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;

                    // Check if this line starts a new row (e.g., "Row 1", "Row 2", etc.)
                    if (trimmedLine.StartsWith("Row ", StringComparison.OrdinalIgnoreCase) &&
                        !trimmedLine.Contains(':'))
                    {
                        // Save the previous row data if it exists
                        if (currentRowData != null && currentRowData.Count > 0)
                        {
                            rowDataList.Add(currentRowData);
                        }

                        // Start a new row
                        currentRowData = new Dictionary<string, string>();
                        currentRowData["RowIdentifier"] = trimmedLine;
                    }
                    else
                    {
                        // Parse key-value pair
                        var colonIndex = trimmedLine.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            var key = trimmedLine.Substring(0, colonIndex).Trim();
                            var value = colonIndex < trimmedLine.Length - 1
                                ? trimmedLine.Substring(colonIndex + 1).Trim()
                                : string.Empty;

                            if (!string.IsNullOrEmpty(key))
                            {
                                // If no current row data exists, create one (for data without explicit row headers)
                                if (currentRowData == null)
                                {
                                    currentRowData = new Dictionary<string, string>();
                                }

                                // Ensure value is never null - use empty string if null or empty
                                currentRowData[key] = string.IsNullOrEmpty(value) ? string.Empty : value;
                            }
                        }
                    }
                }

                // Add the last row if it exists
                if (currentRowData != null && currentRowData.Count > 0)
                {
                    rowDataList.Add(currentRowData);
                }
                return rowDataList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse output data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finds a test step that contains "Navigate to" and optionally matches the search text
        /// </summary>
        /// <param name="searchText">Text to search for in test steps</param>
        /// <returns>Matching test step that contains "Navigate to" or null if not found</returns>
        private TestStep? GetNavigationStep(string searchText)
        {
            // First, try to find a step that contains both "Navigate to" and the search text
            var stepWithBoth = TestCaseInfo?.Steps?.FirstOrDefault(step =>
                (step.Action?.Contains("Navigate to", StringComparison.OrdinalIgnoreCase) == true) &&
                ((step.Action?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                 (step.ExpectedResult?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)));

            if (stepWithBoth != null)
                return stepWithBoth;

            // If not found, try to find any step that contains "Navigate to"
            return TestCaseInfo?.Steps?.FirstOrDefault(step =>
                step.Action?.Contains("Navigate to", StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Finds a test step that matches the given search text using linear search
        /// </summary>
        /// <param name="searchText">Text to search for in test steps</param>
        /// <returns>Matching test step or null if not found</returns>
        private TestStep? GetMatchingStep(string searchText)
        {
            return TestCaseInfo?.Steps?.FirstOrDefault(step =>
                (step.Action?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (step.ExpectedResult?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));
        }

        /// <summary>
        /// Retrieves a specific value from a collection of parsed data dictionaries using key-value lookup with 1-based row indexing.
        /// If the value starts with "@", it retrieves the parameter value using GetParameterValue.
        /// </summary>
        /// <param name="dictionaries">The collection of dictionaries containing parsed row data, typically returned from ParseInputData or ParseOutputData methods</param>        
        /// <param name="key">The field name/key to search for within the dictionary (case-sensitive)</param>
        /// <param name="index">The 1-based row index to retrieve data from (default: 1 for first row)</param>
        /// <param name="parameterIndex">The 1-based row index to retrieve parameter from (default: 1 for first row)</param>
        /// <returns>The string value associated with the specified key in the dictionary at the given index, or parameter value if value starts with "@" at the given index</returns>
        public string GetValue(IList<Dictionary<string, string>> dictionaries, string key, int index = 1, int parameterIndex = 1)
        {
            if (dictionaries is null)
                throw new ArgumentNullException(nameof(dictionaries), "The list of dictionaries cannot be null.");

            if (index - 1 < 0 || index > dictionaries.Count)
                throw new InvalidOperationException($"No data present for the step for row {index}. Please check the steps in azure.");

            var dict = dictionaries[index - 1];

            if (dict.Count == 0)
                throw new InvalidOperationException("No data present for the step. Please check the steps in azure.");

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be a non-empty, non-whitespace string.", nameof(key));

            if (!dict.TryGetValue(key, out var value))
            {
                var sb = new StringBuilder();
                sb.Append($"'{key}' is not found in the Steps for row {index}. Steps contains {dict.Count} entries:");
                foreach (var kvp in dict)
                {
                    sb.Append($" {kvp.Key}|");
                }
                Utilities.Logger.Log(Logger.LogType.Info, sb.ToString(), false);
                return "";
            }

            // If the value starts with "@", get the parameter value using GetParameterValue with the same key and index
            if (!string.IsNullOrEmpty(value) && value.StartsWith("@"))
            {
                try
                {
                    var parameterValue = GetParameterValue(key, parameterIndex);
                    return parameterValue ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to retrieve parameter value for key '{key}' at index {index}: {ex.Message}", ex);
                }
            }
            return value;
        }

        #endregion
    }
}
