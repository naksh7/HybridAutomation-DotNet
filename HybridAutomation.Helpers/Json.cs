using System.Text.Json;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Utility helper for JSON parsing and property extraction.
    /// </summary>
    public class Json
    {
        /// <summary>
        /// Parses a JSON string and extracts the string value of a specified top-level property.
        /// </summary>
        /// <param name="content">The raw JSON string content.</param>
        /// <param name="propertyName">The name of the property to extract.</param>
        /// <returns>The string value of the property, or empty string if null.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property is not found in the JSON content.</exception>
        public string ExtractStringProperty(string content, string propertyName)
        {
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty(propertyName, out var property))
            {
                throw new InvalidOperationException($"Property '{propertyName}' was not found in the response content.");
            }
            return property.GetString() ?? string.Empty;
        }
    }
}
