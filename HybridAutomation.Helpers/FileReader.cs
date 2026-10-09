using System.Text.Json;
using System.Xml.Linq;

namespace HybridAutomation.Helpers
{
    public class FileReader
    {
        /// <summary>
        /// Reads the Json data from text file and deserialise it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public T? ReadConfig<T>(string filePath)
        {
            string json = GetTextFromFile(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Get the value for provided xml file, tag and key
        /// </summary>
        /// <param name="xmlFilePath"></param>
        /// <param name="tag"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetXMLValue(string xmlFilePath, string tag, string key)
        {
            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException($"Config file not found: {xmlFilePath}");

            XDocument doc = XDocument.Load(xmlFilePath);
            var value = doc.Root?.Element(tag)?.Attribute(key)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"No value present for tag '{tag}' and attribute '{key}'.");
            return value;
        }

        /// <summary>
        /// Gets text from provided file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public string GetTextFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Config file not found: {filePath}");

            string text = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception($"No text present for file filePath");
            return text;
        }
    }
}
