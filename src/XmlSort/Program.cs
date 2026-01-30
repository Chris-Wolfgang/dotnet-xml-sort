using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace XmlSort;

[Command(Name = "xmlsort", Description = "Sorts XML nodes in files recursively")]
public class Program
{
    [Argument(0, Description = "File name or filter (e.g., *.csproj)")]
    [Required]
    public string FilePattern { get; set; } = string.Empty;

    [Option("-r|--recursive", Description = "Search subfolders recursively")]
    public bool Recursive { get; set; }

    public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

    private void OnExecute()
    {
        try
        {
            var files = FindFiles(FilePattern, Recursive);
            
            if (!files.Any())
            {
                Console.WriteLine($"No files found matching pattern: {FilePattern}");
                return;
            }

            Console.WriteLine($"Found {files.Count} file(s) matching pattern '{FilePattern}'");
            
            int processedCount = 0;
            int skippedCount = 0;

            foreach (var file in files)
            {
                if (ProcessXmlFile(file))
                {
                    processedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            Console.WriteLine($"\nCompleted:");
            Console.WriteLine($"  - Processed: {processedCount} file(s)");
            Console.WriteLine($"  - Skipped: {skippedCount} file(s)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    internal List<string> FindFiles(string pattern, bool recursive)
    {
        var files = new List<string>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        // Determine if the pattern is a specific file or a wildcard pattern
        string directory = Directory.GetCurrentDirectory();
        string searchPattern = pattern;
        
        if (Path.IsPathRooted(pattern))
        {
            directory = Path.GetDirectoryName(pattern) ?? directory;
            searchPattern = Path.GetFileName(pattern);
            if (string.IsNullOrEmpty(searchPattern))
            {
                return files;
            }
        }
        else if (pattern.Contains(Path.DirectorySeparatorChar) || pattern.Contains(Path.AltDirectorySeparatorChar))
        {
            var dirPart = Path.GetDirectoryName(pattern);
            if (!string.IsNullOrEmpty(dirPart))
            {
                directory = Path.Combine(directory, dirPart);
                searchPattern = Path.GetFileName(pattern);
                if (string.IsNullOrEmpty(searchPattern))
                {
                    return files;
                }
            }
        }

        if (Directory.Exists(directory))
        {
            files.AddRange(Directory.GetFiles(directory, searchPattern, searchOption));
        }

        return files;
    }

    internal bool ProcessXmlFile(string filePath)
    {
        Console.WriteLine($"\nProcessing: {filePath}");
        
        try
        {
            // Try to load as XML
            var doc = XDocument.Load(filePath);
            
            // Sort the XML recursively
            SortXmlNodes(doc.Root);
            
            // Write the sorted XML back to the file
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = doc.Declaration == null
            };

            using var writer = XmlWriter.Create(filePath, settings);
            doc.Save(writer);
            
            Console.WriteLine($"  ✓ Sorted and saved");
            return true;
        }
        catch (XmlException)
        {
            Console.WriteLine($"  ⊗ Skipped (not valid XML)");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            return false;
        }
    }

    internal void SortXmlNodes(XElement? element)
    {
        if (element == null) return;

        // Get and preserve text content
        var textNodes = element.Nodes().OfType<XText>().ToList();
        
        // Sort child elements by name, then by attributes
        // Note: Secondary sort by attributes ensures consistent ordering when multiple
        // elements have the same name but different attribute values
        var sortedElements = element.Elements()
            .OrderBy(e => e.Name.LocalName)
            .ThenBy(e => string.Join(",", e.Attributes().Select(a => $"{a.Name}={a.Value}")))
            .ToList();

        // Sort attributes on the current element
        var sortedAttributes = element.Attributes()
            .OrderBy(a => a.Name.LocalName)
            .ToList();

        // Remove all elements and attributes
        element.RemoveNodes();
        element.RemoveAttributes();

        // Add sorted attributes back
        foreach (var attr in sortedAttributes)
        {
            element.Add(new XAttribute(attr.Name, attr.Value));
        }

        // Add text nodes back first (to preserve original position)
        foreach (var textNode in textNodes)
        {
            element.Add(new XText(textNode.Value));
        }

        // Recursively sort and add elements back
        foreach (var child in sortedElements)
        {
            SortXmlNodes(child);
            element.Add(child);
        }
    }
}
