using System.Xml;
using System.Xml.Linq;

namespace HybridAutomation.Helpers
{
    /// <summary>
    /// Dynamic XPath generation and element location utilities for desktop automation.    
    /// </summary>
    public class Xpath
    {
        // Add max depth to prevent stack overflow
        private const int MAX_RECURSION_DEPTH = 100;

        /// <summary>
        /// Represents a found element with its XPath and details
        /// </summary>
        public class ElementMatch
        {
            public XmlNode? Node { get; set; }
            public List<XmlNode>? ParentPath { get; set; }
            public string? XPath { get; set; }
            public string? MatchedAttribute { get; set; }
            public string? MatchedValue { get; set; }
        }

        /// <summary>
        /// Recursively finds ALL XML elements by Name, AutomationId, ClassName, or other identifying attributes.
        /// </summary>
        /// <param name="current">Current XML node to search</param>
        /// <param name="name">Element name, AutomationId, ClassName, or other attribute value to find</param>
        /// <param name="parentNodes">List to store parent nodes path</param>
        /// <param name="allMatches">List to store all found matches</param>
        /// <param name="depth">Current recursion depth to prevent stack overflow</param>
        private void FindAllElementsByName(XmlNode current, string name, List<XmlNode> parentNodes, List<ElementMatch> allMatches, int depth = 0)
        {
            try
            {
                if (current == null || depth > MAX_RECURSION_DEPTH)
                {
                    return;
                }

                // Add input validation
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                bool matchFound = false;
                string matchedAttribute = string.Empty;

                if (current.Attributes != null)
                {
                    // List of attributes to search through
                    string[] searchAttributes = {
                        "Name",
                        "AutomationId",
                        "ClassName",
                        "HelpText",
                        "LocalizedControlType",
                        "ControlType",
                        "AccessKey",
                        "AcceleratorKey"
                    };

                    foreach (string attrName in searchAttributes)
                    {
                        XmlAttribute? attribute = current.Attributes[attrName];
                        if (attribute?.Value == name)
                        {
                            matchFound = true;
                            matchedAttribute = attrName;
                            break;
                        }
                    }
                }

                if (matchFound)
                {
                    // Create a copy of parent nodes for this match
                    List<XmlNode> currentPath = new List<XmlNode>(parentNodes) { current };

                    string xpath = "/";
                    foreach (XmlNode node in currentPath)
                    {
                        xpath += GetNodeXPath(node);
                    }

                    allMatches.Add(new ElementMatch
                    {
                        Node = current,
                        ParentPath = currentPath,
                        XPath = xpath,
                        MatchedAttribute = matchedAttribute,
                        MatchedValue = name
                    });
                }

                // Continue searching in child nodes regardless of whether we found a match
                if (current.ChildNodes != null)
                {
                    foreach (XmlNode childNode in current.ChildNodes)
                    {
                        parentNodes.Add(current);
                        FindAllElementsByName(childNode, name, parentNodes, allMatches, depth + 1);
                        parentNodes.Remove(current);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nFindAllElementsByName failed for name: {name} at depth: {depth}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Generates XPath expression for XML node using all available attributes with separate brackets.
        /// </summary>
        /// <param name="node">XML node to generate XPath for</param>
        /// <returns>XPath string for the node with separate attribute brackets</returns>
        private string GetNodeXPath(XmlNode node)
        {
            try
            {
                if (node == null)
                {
                    return string.Empty;
                }

                string nodeName = node.Name;
                string xpath = "/" + nodeName;

                List<string> attributeBrackets = new List<string>();

                // Primary attributes for element identification
                string[] primaryAttributes = { "Name", "ClassName", "AutomationId" };

                if (node.Attributes != null)
                {
                    foreach (string attrName in primaryAttributes)
                    {
                        XmlAttribute? attribute = node.Attributes[attrName];
                        string? attributeValue = attribute?.Value;

                        if (!string.IsNullOrEmpty(attributeValue))
                        {
                            // Skip numeric AutomationId values as they are often not reliable
                            if (attrName == "AutomationId" && int.TryParse(attributeValue, out _))
                            {
                                continue;
                            }

                            // Escape single quotes in XPath
                            string escapedValue = attributeValue.Replace("'", "&apos;");
                            attributeBrackets.Add($"[@{attrName}='{escapedValue}']");
                        }
                    }

                    // Add additional useful attributes if primary ones are not sufficient
                    if (attributeBrackets.Count == 0)
                    {
                        string[] secondaryAttributes = { "HelpText", "LocalizedControlType", "ControlType" };

                        foreach (string attrName in secondaryAttributes)
                        {
                            XmlAttribute? attribute = node.Attributes[attrName];
                            string? attributeValue = attribute?.Value;

                            if (!string.IsNullOrEmpty(attributeValue))
                            {
                                string escapedValue = attributeValue.Replace("'", "&apos;");
                                attributeBrackets.Add($"[@{attrName}='{escapedValue}']");
                                break; // Use only one secondary attribute to keep XPath simple
                            }
                        }
                    }
                }

                // Combine all attribute brackets separately
                if (attributeBrackets.Count > 0)
                {
                    xpath += string.Join("", attributeBrackets);
                }

                return xpath;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetNodeXPath failed for node: {node?.Name}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Removes unwanted XML attributes, keeping useful attributes for element identification.
        /// </summary>
        /// <param name="element">XML element to clean</param>
        /// <returns>Cleaned XML element</returns>
        private XElement RemoveUnwantedTags(XElement element)
        {
            try
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                XElement cleanedElement = new XElement(element.Name.LocalName);

                // List of attributes to preserve for element identification
                string[] preservedAttributes = {
                    "Name",
                    "AutomationId",
                    "ClassName"
                };

                foreach (string attrName in preservedAttributes)
                {
                    XAttribute? attr = element.Attribute(attrName);
                    if (attr != null)
                    {
                        cleanedElement.Add(new XAttribute(attrName, attr.Value));
                    }
                }

                foreach (XElement childElement in element.Elements())
                {
                    cleanedElement.Add(RemoveUnwantedTags(childElement));
                }

                return cleanedElement;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nRemoveUnwantedTags failed for element: {element?.Name}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Simplifies XML by removing unwanted attributes and elements.
        /// </summary>
        /// <param name="xml">Original XML string</param>
        /// <returns>Simplified XML string</returns>
        private string SimplifyXML(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    throw new ArgumentException("XML content cannot be null or empty", nameof(xml));
                }

                XElement element = XElement.Parse(xml);
                XElement xelement = RemoveUnwantedTags(element);
                return xelement.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nSimplifyXML failed for xml with length: {xml?.Length ?? 0}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Saves simplified XML to file for analysis and debugging.
        /// </summary>
        /// <param name="xml">XML content to simplify and save</param>
        /// <param name="filePath">Directory path to save the file</param>
        public void SaveSimplifiedXML(string xml, string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    throw new ArgumentException("XML content cannot be null or empty", nameof(xml));
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                XElement element = XElement.Parse(xml);
                XElement xelement = RemoveUnwantedTags(element);
                Utilities.Files.EnsureDirectoryExist(filePath);
                string fullPath = Path.Combine(filePath, "SimplifiedTree.xml");
                
                xelement.Save(fullPath);
                Console.WriteLine($"Simplified XML has been saved to {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving simplified XML file: {ex.Message}");
                throw new Exception($"{ex.Message}\nSaveSimplifiedXML failed for xml with length: {xml?.Length ?? 0} and filePath: {filePath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds and displays ALL XPaths for elements matching the locator.
        /// </summary>
        /// <param name="locatorToFind">Element attribute value to locate (Name, AutomationId, ClassName, etc.)</param>
        /// <param name="pageSource">XML page source to search in</param>
        public void XpathFinderAll(string locatorToFind, string pageSource)
        {
            try
            {
                string xml = SimplifyXML(pageSource);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);

                // Ensure xmlDocument.DocumentElement is not null before calling FindAllElementsByName
                if (xmlDocument.DocumentElement == null)               
                    throw new InvalidOperationException("The XML document does not have a root element.");
                
                List<ElementMatch> allMatches = new();
                FindAllElementsByName(xmlDocument.DocumentElement!, locatorToFind, new List<XmlNode>(), allMatches);

                if (allMatches.Count > 0)
                {
                    Console.WriteLine($"Found {allMatches.Count} element(s) with attribute value '{locatorToFind}':");
                    Console.WriteLine(new string('=', 80));

                    for (int i = 0; i < allMatches.Count; i++)
                    {
                        ElementMatch match = allMatches[i];
                        Console.WriteLine($"\nMatch #{i + 1}:");
                        Console.WriteLine($"Matched Attribute: {match.MatchedAttribute}");
                        Console.WriteLine($"Element Type: {match.Node?.Name}");

                        // Display all attributes of the matched element
                        if (match.Node?.Attributes != null && match.Node.Attributes.Count > 0)
                        {
                            Console.WriteLine("Element Attributes:");
                            foreach (XmlAttribute attr in match.Node.Attributes)
                            {
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    Console.WriteLine($"  {attr.Name}: '{attr.Value}'");
                                }
                            }
                        }

                        Console.WriteLine($"XPath: {match.XPath}");
                        Console.WriteLine(new string('-', 40));
                    }
                }
                else
                {
                    Console.WriteLine($"No elements found with attribute value '{locatorToFind}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nXpathFinderAll failed for locatorToFind: {locatorToFind} and pageSource with length: {pageSource?.Length ?? 0}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Finds and displays XPaths for ALL matches of multiple elements from provided list.
        /// </summary>
        /// <param name="searchList">List of element attribute values to locate</param>
        /// <param name="pageSource">XML page source to search in</param>
        public void XpathsFinderAll(List<string> searchList, string pageSource)
        {
            try
            {
                string xml = SimplifyXML(pageSource);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);

                // Ensure xmlDocument.DocumentElement is not null before proceeding
                if (xmlDocument.DocumentElement == null)                
                    throw new InvalidOperationException("The XML document does not have a root element.");
               
                Console.WriteLine("Searching for ALL matches of provided elements:");
                Console.WriteLine(new string('=', 80));

                foreach (string searchTerm in searchList)
                {
                    List<ElementMatch> allMatches = new List<ElementMatch>();
                    FindAllElementsByName(xmlDocument.DocumentElement, searchTerm, new List<XmlNode>(), allMatches);

                    Console.WriteLine($"\nSearching for: '{searchTerm}'");
                    Console.WriteLine(new string('-', 50));

                    if (allMatches.Count > 0)
                    {
                        Console.WriteLine($"Found {allMatches.Count} match(es):");

                        for (int i = 0; i < allMatches.Count; i++)
                        {
                            ElementMatch match = allMatches[i];
                            Console.WriteLine($"\n  Match #{i + 1} ({match.MatchedAttribute}):");
                            Console.WriteLine($"  string xpath{i + 1} = \"{match.XPath}\";");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  No elements found with attribute value '{searchTerm}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nXpathsFinderAll failed for searchList with {searchList?.Count ?? 0} items and pageSource with length: {pageSource?.Length ?? 0}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Returns all matches for a given locator without printing to console.
        /// Useful for programmatic access to results.
        /// </summary>
        /// <param name="locatorToFind">Element attribute value to locate</param>
        /// <param name="pageSource">XML page source to search in</param>
        /// <returns>List of all matching elements</returns>
        public List<ElementMatch> GetAllMatches(string locatorToFind, string pageSource)
        {
            try
            {
                string xml = SimplifyXML(pageSource);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                List<ElementMatch> allMatches = new List<ElementMatch>();
                if (xmlDocument.DocumentElement == null)               
                    throw new InvalidOperationException("The XML document does not have a root element.");
               
                FindAllElementsByName(xmlDocument.DocumentElement, locatorToFind, new List<XmlNode>(), allMatches);

                return allMatches;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetAllMatches failed for locatorToFind: {locatorToFind} and pageSource with length: {pageSource?.Length ?? 0}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Prints a summary of all unique elements found in the XML.
        /// </summary>
        /// <param name="pageSource">XML page source to analyze</param>
        public void PrintElementSummary(string pageSource)
        {
            try
            {
                string xml = SimplifyXML(pageSource);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);

                Dictionary<string, int> elementCounts = new Dictionary<string, int>();
                Dictionary<string, HashSet<string>> elementAttributes = new Dictionary<string, HashSet<string>>();

                // Ensure xmlDocument.DocumentElement is not null before calling CountElements
                if (xmlDocument.DocumentElement != null)               
                    CountElements(xmlDocument.DocumentElement, elementCounts, elementAttributes);
                else               
                    throw new InvalidOperationException("The XML document does not have a root element.");
                CountElements(xmlDocument.DocumentElement, elementCounts, elementAttributes);

                Console.WriteLine("Element Summary:");
                Console.WriteLine(new string('=', 50));

                foreach (var kvp in elementCounts.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value} occurrences");
                    if (elementAttributes.ContainsKey(kvp.Key))                   
                        Console.WriteLine($"  Common attributes: {string.Join(", ", elementAttributes[kvp.Key])}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nPrintElementSummary failed for pageSource with length: {pageSource?.Length ?? 0}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Helper method to count elements and their attributes.
        /// </summary>
        private void CountElements(XmlNode node, Dictionary<string, int> elementCounts, Dictionary<string, HashSet<string>> elementAttributes)
        {
            try
            {
                if (node == null) return;

                string nodeName = node.Name;

                if (!elementCounts.ContainsKey(nodeName))
                {
                    elementCounts[nodeName] = 0;
                    elementAttributes[nodeName] = new HashSet<string>();
                }

                elementCounts[nodeName]++;

                if (node.Attributes != null)
                {
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (!string.IsNullOrEmpty(attr.Value))
                        {
                            elementAttributes[nodeName].Add(attr.Name);
                        }
                    }
                }

                foreach (XmlNode child in node.ChildNodes)
                {
                    CountElements(child, elementCounts, elementAttributes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nCountElements failed for node: {node?.Name}\n{ex.StackTrace}", ex);
            }
        }
    }
}