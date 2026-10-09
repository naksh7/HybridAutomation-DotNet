using System.Text.RegularExpressions;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Utility helper for regular expression pattern matching and group extraction.
    /// </summary>
    public class Regexp
    {
        /// <summary>
        /// Extracts the first captured group from the input string matching the specified regex pattern.
        /// </summary>
        /// <param name="input">The input string to evaluate.</param>
        /// <param name="pattern">The regular expression pattern containing at least one capture group.</param>
        /// <returns>The string value of the first capture group.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pattern does not match or contains no capture groups.</exception>
        public string ExtractFirstGroup(string input, string pattern)
        {            
            var match = Regex.Match(input, pattern);
            if (!match.Success || match.Groups.Count < 2)
            {
                throw new InvalidOperationException($"Pattern '{pattern}' did not match the response content.");
            }

            return match.Groups[1].Value;
        }

        /// <summary>
        /// Attempts to extract the first captured group from the input string matching the specified regex pattern.
        /// </summary>
        /// <param name="input">The input string to evaluate.</param>
        /// <param name="pattern">The regular expression pattern containing at least one capture group.</param>
        /// <param name="value">When this method returns, contains the value of the first capture group if successful; otherwise, empty string.</param>
        /// <returns><c>true</c> if a match and capture group were found; otherwise, <c>false</c>.</returns>
        public bool TryExtractFirstGroup(string input, string pattern, out string value)
        {
            var match = Regex.Match(input, pattern);
            if (!match.Success || match.Groups.Count < 2)
            {
                value = string.Empty;
                return false;
            }

            value = match.Groups[1].Value;
            return true;
        }
    }
}
