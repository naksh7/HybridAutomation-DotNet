using RestSharp;
using RestSharp.Authenticators;
using System.Diagnostics;

namespace HybridAutomation.Helpers
{
    public class ApiResponse
    {
        public int? StatusCode { get; init; }
        public bool? IsSuccessful { get; init; }
        public string Content { get; init; } = string.Empty;
        public long? ResponseTimeMS { get; init; }
        public List<ApiHeader> Headers { get; init; } = new();
        public string? ErrorMessage { get; init; }

        /// <summary>Throws if actual response doesn't match expected values (unset fields default: 200, true, no time check).</summary>
        public ApiResponse EnsureApiResponse(ApiResponse? expectedApiResponse = null)
        {
            int expectedStatusCode = expectedApiResponse?.StatusCode ?? 200;
            bool expectedIsSuccessful = expectedApiResponse?.IsSuccessful ?? true;

            if (IsSuccessful != expectedIsSuccessful)
            {
                throw new InvalidOperationException(
                    $"Expected IsSuccessful={expectedIsSuccessful} but got {IsSuccessful}. Response: {Content}");
            }

            if (StatusCode != expectedStatusCode)
            {
                throw new InvalidOperationException(
                    $"Expected status code {expectedStatusCode} but got {StatusCode}. Response: {Content}");
            }            

            if (expectedApiResponse != null && ResponseTimeMS > expectedApiResponse.ResponseTimeMS)
            {
                throw new InvalidOperationException(
                    $"Expected response time <= {expectedApiResponse.ResponseTimeMS}ms but got {ResponseTimeMS}ms.");
            }
            return this;
        }
    }

    public class ApiHeader
    {
        public string Name { get; init; } = string.Empty;
        public string? Value { get; init; }
    }

    /// <summary>
    /// Thin, reusable wrapper around RestSharp exposing every HTTP verb and auth/body style
    /// needed by the API test suites (JSON, url-encoded form, multipart file upload, bearer auth).
    /// </summary>
    public class RestSharp : IDisposable
    {
        private readonly RestClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestSharp"/> class with default client options.
        /// </summary>
        public RestSharp()
        {
            _client = new RestClient(new RestClientOptions
            {
                ThrowOnAnyError = false,
                FollowRedirects = true,
                UserAgent = "Mozilla/5.0 (compatible; RestSharpAutomation/1.0)"
            });
        }

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="queryParams">Optional query parameters as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse Get(string url, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null)
            => Execute(url, Method.Get, headers: headers, queryParams: queryParams, bearerToken: bearerToken);

        /// <summary>
        /// Sends a POST request with an optional JSON body to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="jsonBody">Optional payload object to serialize as JSON.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse Post(string url, object? jsonBody = null, Dictionary<string, string>? headers = null, string? bearerToken = null)
        {
            return Execute(url, Method.Post, jsonBody: jsonBody, headers: headers, bearerToken: bearerToken);
        }

        /// <summary>
        /// Sends a PUT request with an optional JSON body to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="jsonBody">Optional payload object to serialize as JSON.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse Put(string url, object? jsonBody = null, Dictionary<string, string>? headers = null, string? bearerToken = null)
        {
            return Execute(url, Method.Put, jsonBody: jsonBody, headers: headers, bearerToken: bearerToken);
        }
            
        /// <summary>
        /// Sends a PATCH request with an optional JSON body to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="jsonBody">Optional payload object to serialize as JSON.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse Patch(string url, object? jsonBody = null, Dictionary<string, string>? headers = null, string? bearerToken = null)
        {
            return Execute(url, Method.Patch, jsonBody: jsonBody, headers: headers, bearerToken: bearerToken);
        }
         
        /// <summary>
        /// Sends a DELETE request with an optional JSON body to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="jsonBody">Optional payload object to serialize as JSON.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse Delete(string url, Dictionary<string, string>? headers = null, object? jsonBody = null, string? bearerToken = null)
        {
            return Execute(url, Method.Delete, jsonBody: jsonBody, headers: headers, bearerToken: bearerToken);
        }

        /// <summary>
        /// Sends a POST request with URL-encoded form parameters to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="formParams">Dictionary containing key-value form fields.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse PostUrlEncoded(string url, Dictionary<string, string> formParams, Dictionary<string, string>? headers = null, string? bearerToken = null)
        {
            var request = BuildRequest(url, Method.Post, headers, bearerToken: bearerToken);
            foreach (var (key, value) in formParams)
            {
                request.AddParameter(key, value);
            }

            return ExecuteRequest(request);
        }

        /// <summary>
        /// Sends a multipart/form-data POST request with a file upload to the specified URL.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="paramName">The form field name for the uploaded file.</param>
        /// <param name="filePath">The absolute or relative path to the file to upload.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <param name="additionalFields">Optional additional form key-value fields.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse PostFormFile(string url, string paramName, string filePath, Dictionary<string, string>? headers = null, string? bearerToken = null, Dictionary<string, string>? additionalFields = null)
        {
            var request = BuildRequest(url, Method.Post, headers, bearerToken: bearerToken);
            request.AddFile(paramName, filePath);

            if (additionalFields != null)
            {
                foreach (var (key, value) in additionalFields)
                {
                    request.AddParameter(key, value);
                }
            }
            return ExecuteRequest(request);
        }

        /// <summary>
        /// Sends a body exactly as provided, without re-serializing it (mirrors Postman's raw body mode).
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="rawBody">The raw string payload to send.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <param name="contentType">Optional content type for the raw body. Defaults to JSON if null.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        public ApiResponse PostRaw(string url, string rawBody, Dictionary<string, string>? headers = null, string? bearerToken = null, ContentType? contentType = null)
        {
            var request = BuildRequest(url, Method.Post, headers, bearerToken: bearerToken);
            request.AddStringBody(rawBody, contentType ?? ContentType.Json);
            return ExecuteRequest(request);
        }

        /// <summary>
        /// Executes an HTTP request with the specified method, body, headers, query parameters, and auth token.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</param>
        /// <param name="jsonBody">Optional payload object to serialize as JSON.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="queryParams">Optional query parameters as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        private ApiResponse Execute(string url, Method method, object? jsonBody = null, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null)
        {
            var request = BuildRequest(url, method, headers, queryParams, bearerToken);
            if (jsonBody != null)
            {
                request.AddJsonBody(jsonBody);
            }
            return ExecuteRequest(request);
        }

        /// <summary>
        /// Builds and configures a <see cref="RestRequest"/> instance with headers, query parameters, and authentication.
        /// </summary>
        /// <param name="url">The request endpoint URL.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="headers">Optional request headers as key-value pairs.</param>
        /// <param name="queryParams">Optional query parameters as key-value pairs.</param>
        /// <param name="bearerToken">Optional bearer JWT token for authorization.</param>
        /// <returns>A configured <see cref="RestRequest"/> instance ready for execution.</returns>
        private RestRequest BuildRequest(string url, Method method, Dictionary<string, string>? headers, Dictionary<string, string>? queryParams = null, string? bearerToken = null)
        {
            var request = new RestRequest(url, method);

            // RestSharp's default Accept header (json/xml only) makes some servers/WAFs 500 on non-API endpoints; allow html by default.
            request.AddOrUpdateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7");

            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.Authenticator = new JwtAuthenticator(bearerToken);
            }

            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    request.AddOrUpdateHeader(key, value);
                }
            }
            if (queryParams != null)
            {
                foreach (var (key, value) in queryParams)
                {
                    request.AddQueryParameter(key, value);
                }
            }
            return request;
        }

        /// <summary>
        /// Executes the configured <see cref="RestRequest"/> and measures the response time.
        /// </summary>
        /// <param name="request">The configured <see cref="RestRequest"/> instance to execute.</param>
        /// <returns>An <see cref="ApiResponse"/> containing status code, response body, headers, and execution metrics.</returns>
        private ApiResponse ExecuteRequest(RestRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = _client.Execute(request);
            stopwatch.Stop();

            return new ApiResponse
            {
                StatusCode = (int)response.StatusCode,
                IsSuccessful = response.IsSuccessful,
                Content = response.Content ?? string.Empty,
                ResponseTimeMS = stopwatch.ElapsedMilliseconds,
                ErrorMessage = response.ErrorMessage,
                Headers = response.Headers?
                    .Select(h => new ApiHeader { Name = h.Name ?? string.Empty, Value = h.Value?.ToString() })
                    .ToList() ?? new List<ApiHeader>()
            };
        }

        /// <summary>
        /// Disposes the underlying <see cref="RestClient"/> instance.
        /// </summary>
        public void Dispose()
        {           
            _client.Dispose();
        }
    }
}
