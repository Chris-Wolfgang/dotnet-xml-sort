using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace XmlSort;

[Command(Name = "sort-xml", Description = "Sorts XML nodes in files recursively")]
public class Program
{
    [Argument(0, Description = "File name or filter (e.g., *.csproj)")]
    [Required]
    public string FilePattern { get; set; } = string.Empty;

    [Option("-r|--recursive", Description = "Search subfolders recursively")]
    public bool Recursive { get; set; }

    [Option("--no-backup", Description = "Do not create a backup before overwriting")]
    public bool NoBackup { get; set; }

    [Option("-cs|--case-sensitive", Description = "Sort using current culture in a case-sensitive way (default is case-insensitive)")]
    public bool CaseSensitive { get; set; }

    [Option("-sa|--sort-attributes", Description = "Also sort each element's attributes (default preserves attribute order)")]
    public bool SortAttributes { get; set; }

    [Option("--remove-dupes", Description = "Remove duplicate sibling elements with the same name and attributes (except PropertyGroup and ItemGroup)")]
    public bool RemoveDupes { get; set; }

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

    /// <summary>
    /// Efficiently determines if a file is XML by reading at most 512 bytes.
    /// The fixed-size prefix is copied into a MemoryStream so the XmlReader
    /// can never read beyond that hard cap, regardless of BOM/whitespace/DTD.
    /// </summary>
    internal bool IsXmlFile(string filePath)
    {
        try
        {
            // Read only a fixed-size prefix from the file to enforce a hard cap on bytes read.
            byte[] buffer = new byte[512];
            int bytesRead;

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 512))
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
            }

            if (bytesRead == 0)
            {
                return false;
            }

            using var memoryStream = new MemoryStream(buffer, 0, bytesRead, writable: false);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(memoryStream, settings);
            reader.MoveToContent();
            return reader.NodeType == XmlNodeType.Element;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a backup of the file before overwriting.
    /// Uses filename-backup.ext, then filename-backup2.ext, etc.
    /// Returns the backup path, or null if NoBackup is set.
    /// </summary>
    internal string? CreateBackup(string filePath)
    {
        if (NoBackup) return null;

        var dir = Path.GetDirectoryName(filePath) ?? ".";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        var backupPath = Path.Combine(dir, $"{nameWithoutExt}-backup{ext}");
        int counter = 2;
        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(dir, $"{nameWithoutExt}-backup{counter}{ext}");
            counter++;
        }

        File.Copy(filePath, backupPath);
        return backupPath;
    }

    internal bool ProcessXmlFile(string filePath)
    {
        Console.WriteLine($"\nProcessing: {filePath}");

        if (!IsXmlFile(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"  ✗ Not valid XML, skipping: {filePath}");
            Console.ResetColor();
            return false;
        }

        try
        {
            var doc = XDocument.Load(filePath, LoadOptions.SetLineInfo);

            // Sort the XML recursively
            SortXmlNodes(doc.Root);

            // Create backup before overwriting
            var backupPath = CreateBackup(filePath);
            if (backupPath != null)
            {
                Console.WriteLine($"  → Backup created: {backupPath}");
            }

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
        catch (XmlException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"  ✗ Malformed XML, skipping: {ex.Message}");
            Console.ResetColor();
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  ✗ Error: {ex.Message}");
            return false;
        }
    }

    internal void SortXmlNodes(XElement? element)
    {
        if (element == null) return;

        var comparison = CaseSensitive
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
        var comparer = GetComparer(comparison);

        // Get and preserve text content
        var textNodes = element.Nodes().OfType<XText>().ToList();

        // Snapshot child elements (may be reduced if duplicates are removed)
        var childElements = element.Elements().ToList();

        // Detect and handle duplicate sibling elements
        DetectAndHandleDuplicates(element, comparison, childElements);

        // Sort child elements by name, then by attribute set for stable tie-breaking
        var sortedElements = childElements
            .OrderBy(e => e.Name.LocalName, comparer)
            .ThenBy(e => string.Join(",", e.Attributes().Select(a => $"{a.Name}={a.Value}")), comparer)
            .ToList();

        // Remove all child nodes (text and elements) from the element
        element.RemoveNodes();

        if (SortAttributes)
        {
            // Sort attributes and restore them
            var sortedAttributes = element.Attributes()
                .OrderBy(a => a.Name.LocalName, comparer)
                .ToList();
            element.RemoveAttributes();
            foreach (var attr in sortedAttributes)
            {
                element.Add(new XAttribute(attr.Name, attr.Value));
            }
        }

        // Restore text nodes
        foreach (var textNode in textNodes)
        {
            element.Add(new XText(textNode.Value));
        }

        // Recursively sort and add child elements back
        foreach (var child in sortedElements)
        {
            SortXmlNodes(child);
            element.Add(child);
        }
    }

    /// <summary>
    /// Detects duplicate sibling elements under <paramref name="parent"/>. Two elements are
    /// considered duplicates when their full subtree matches: the element name, all attribute
    /// names and values, and all descendant element names, attribute names, values, and text
    /// content must be identical (using the current case-sensitivity setting).
    /// Emits a yellow warning for each duplicate group. If RemoveDupes is set, removes all but the
    /// first occurrence, unless the element name is PropertyGroup or ItemGroup.
    /// </summary>
    internal void DetectAndHandleDuplicates(XElement parent, StringComparison comparison, List<XElement> elements)
    {
        var comparer = GetComparer(comparison);
        var groups = elements.GroupBy(e => GetElementKey(e, comparison), comparer);

        foreach (var group in groups)
        {
            var count = group.Count();
            if (count <= 1) continue;

            var first = group.First();
            var lineInfo = first as IXmlLineInfo;
            var location = lineInfo?.HasLineInfo() == true
                ? $" (line {lineInfo.LineNumber}, col {lineInfo.LinePosition})"
                : string.Empty;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ Duplicate nodes found{location}: <{first.Name.LocalName}> ({count} occurrences) in <{parent.Name.LocalName}>");
            Console.ResetColor();

            if (RemoveDupes)
            {
                var protectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PropertyGroup", "ItemGroup" };
                if (!protectedNames.Contains(first.Name.LocalName))
                {
                    foreach (var dupe in group.Skip(1).ToList())
                    {
                        elements.Remove(dupe);
                    }
                }
            }
        }
    }

    private static string GetElementKey(XElement element, StringComparison comparison)
    {
        var comparer = GetComparer(comparison);
        var sb = new System.Text.StringBuilder();
        BuildElementKey(element, comparer, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Recursively builds a canonical key for <paramref name="element"/> that encodes the
    /// element name, all attribute names and values, text content, and all descendants.
    /// Control characters \x00-\x07 are used as structural delimiters; they are invalid in
    /// XML 1.0 names, attribute values, and text content so they cannot cause collisions:
    ///   \x00 after element name; \x01 between attr name and value; \x02 after attr value;
    ///   \x03 end-of-attributes; \x04 before child; \x05 after child;
    ///   \x06 before text node; \x07 after text node.
    /// </summary>
    private static void BuildElementKey(XElement element, StringComparer comparer, System.Text.StringBuilder sb)
    {
        // Element name
        sb.Append(element.Name.LocalName).Append('\x00');

        // Attributes sorted by name for a stable, order-independent signature
        foreach (var attr in element.Attributes().OrderBy(a => a.Name.LocalName, comparer))
        {
            sb.Append(attr.Name.LocalName).Append('\x01').Append(attr.Value).Append('\x02');
        }
        sb.Append('\x03'); // end-of-attributes marker

        // Recurse into child elements (in document order)
        foreach (var child in element.Elements())
        {
            sb.Append('\x04');
            BuildElementKey(child, comparer, sb);
            sb.Append('\x05');
        }

        // Direct text nodes of this element only (element.Nodes() returns immediate children,
        // not all descendants, so descendant text is captured during recursion above).
        foreach (var text in element.Nodes().OfType<XText>())
        {
            sb.Append('\x06').Append(text.Value).Append('\x07');
        }
    }

    private static StringComparer GetComparer(StringComparison comparison) =>
        comparison == StringComparison.CurrentCultureIgnoreCase
            ? StringComparer.CurrentCultureIgnoreCase
            : StringComparer.CurrentCulture;
}

