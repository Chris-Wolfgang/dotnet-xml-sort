using System.Xml.Linq;

namespace XmlSort.Tests;

public class XmlSortTests
{
    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"xmlsort-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    [Fact]
    public void Main_WithValidArguments_ReturnsZero()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><z /><a /></root>");

            // Act
            var result = Program.Main(new[] { Path.Combine(tempDir, "*.xml") });

            // Assert
            Assert.Equal(0, result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Main_WithRecursiveFlag_ProcessesSubdirectories()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            
            var testFile1 = Path.Combine(tempDir, "test1.xml");
            var testFile2 = Path.Combine(subDir, "test2.xml");
            
            File.WriteAllText(testFile1, "<root><z /><a /></root>");
            File.WriteAllText(testFile2, "<root><z /><a /></root>");

            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.xml", "-r" });

            // Assert
            Assert.Equal(0, result);
            
            // Verify both files were sorted
            var doc1 = XDocument.Load(testFile1);
            var doc2 = XDocument.Load(testFile2);
            
            Assert.Equal("a", doc1.Root!.Elements().First().Name.LocalName);
            Assert.Equal("a", doc2.Root!.Elements().First().Name.LocalName);

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Main_WithNoMatchingFiles_ReturnsZero()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.nonexistent" });

            // Assert
            Assert.Equal(0, result);

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Main_WithLongRecursiveFlag_ProcessesSubdirectories()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            
            var testFile = Path.Combine(subDir, "test.xml");
            File.WriteAllText(testFile, "<root><z /><a /></root>");

            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.xml", "--recursive" });

            // Assert
            Assert.Equal(0, result);

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SortXmlNodes_SortsElementsByName()
    {
        // Arrange
        var xml = @"<root>
    <zebra>z</zebra>
    <alpha>a</alpha>
    <beta>b</beta>
</root>";
        var doc = XDocument.Parse(xml);

        // Act
        var program = new TestableProgram();
        program.SortXmlNodesPublic(doc.Root);

        // Assert
        var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "beta", "zebra" }, elements);
    }

    [Fact]
    public void SortXmlNodes_SortsAttributesByName()
    {
        // Arrange
        var xml = @"<root zebra=""z"" alpha=""a"" beta=""b"" />";
        var doc = XDocument.Parse(xml);

        // Act
        var program = new TestableProgram();
        program.SortXmlNodesPublic(doc.Root);

        // Assert
        var attributes = doc.Root!.Attributes().Select(a => a.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "beta", "zebra" }, attributes);
    }

    [Fact]
    public void SortXmlNodes_RecursivelySortsNestedElements()
    {
        // Arrange
        var xml = @"<root>
    <parent2>
        <child2 />
        <child1 />
    </parent2>
    <parent1>
        <childZ />
        <childA />
    </parent1>
</root>";
        var doc = XDocument.Parse(xml);

        // Act
        var program = new TestableProgram();
        program.SortXmlNodesPublic(doc.Root);

        // Assert
        var parentElements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "parent1", "parent2" }, parentElements);

        var parent1Children = doc.Root.Element("parent1")!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "childA", "childZ" }, parent1Children);

        var parent2Children = doc.Root.Element("parent2")!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "child1", "child2" }, parent2Children);
    }

    [Fact]
    public void ProcessXmlFile_ValidXml_ReturnsTrueAndSortsFile()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
    <zebra />
    <alpha />
</root>";
            File.WriteAllText(testFile, xml);

            // Act
            var program = new TestableProgram();
            var result = program.ProcessXmlFilePublic(testFile);

            // Assert
            Assert.True(result);
            var doc = XDocument.Load(testFile);
            var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
            Assert.Equal(new[] { "alpha", "zebra" }, elements);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ProcessXmlFile_InvalidXml_ReturnsFalse()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "This is not XML");

            // Act
            var program = new TestableProgram();
            var result = program.ProcessXmlFilePublic(testFile);

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FindFiles_WithWildcard_FindsMatchingFiles()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test1.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test2.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "text");

            var program = new TestableProgram();
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var files = program.FindFilesPublic("*.xml", false);

            // Assert
            Assert.Equal(2, files.Count);
            Assert.All(files, f => Assert.EndsWith(".xml", f));

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FindFiles_WithAbsolutePath_FindsFiles()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            var program = new TestableProgram();

            // Act
            var files = program.FindFilesPublic(Path.Combine(tempDir, "*.xml"), false);

            // Assert
            Assert.Single(files);
            Assert.EndsWith("test.xml", files[0]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FindFiles_WithRelativePath_FindsFiles()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);
            
            var testFile = Path.Combine(subDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            var program = new TestableProgram();

            // Act
            var files = program.FindFilesPublic("subdir/*.xml", false);

            // Assert
            Assert.Single(files);

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FindFiles_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var program = new TestableProgram();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "*.xml");

        // Act
        var files = program.FindFilesPublic(nonExistentPath, false);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void ProcessXmlFile_WithException_ReturnsFalse()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><unclosed>");

            var program = new TestableProgram();

            // Act
            var result = program.ProcessXmlFilePublic(testFile);

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Main_WithMixedValidAndInvalidFiles_ProcessesValid()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var validFile = Path.Combine(tempDir, "valid.xml");
            var invalidFile = Path.Combine(tempDir, "invalid.xml");
            
            File.WriteAllText(validFile, "<root><z /><a /></root>");
            File.WriteAllText(invalidFile, "Not XML content");

            // Act
            var result = Program.Main(new[] { Path.Combine(tempDir, "*.xml") });

            // Assert
            Assert.Equal(0, result);

            // Verify valid file was sorted
            var doc = XDocument.Load(validFile);
            Assert.Equal("a", doc.Root!.Elements().First().Name.LocalName);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FindFiles_Recursive_FindsFilesInSubdirectories()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(tempDir, "test1.xml"), "<root />");
            File.WriteAllText(Path.Combine(subDir, "test2.xml"), "<root />");

            var program = new TestableProgram();
            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var filesNonRecursive = program.FindFilesPublic("*.xml", false);
            var filesRecursive = program.FindFilesPublic("*.xml", true);

            // Assert
            Assert.Single(filesNonRecursive);
            Assert.Equal(2, filesRecursive.Count);

            Directory.SetCurrentDirectory(originalDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SortXmlNodes_PreservesTextContent()
    {
        // Arrange
        var xml = @"<root>
    <element>Some text content</element>
</root>";
        var doc = XDocument.Parse(xml);

        // Act
        var program = new TestableProgram();
        program.SortXmlNodesPublic(doc.Root);

        // Assert
        Assert.Equal("Some text content", doc.Root!.Element("element")!.Value);
    }

    [Fact]
    public void SortXmlNodes_PreservesMixedContent()
    {
        // Arrange
        var xml = @"<root>
    <zebra attr2=""2"" attr1=""1"">
        <nested2 />
        <nested1 />
    </zebra>
    <alpha>Text</alpha>
</root>";
        var doc = XDocument.Parse(xml);

        // Act
        var program = new TestableProgram();
        program.SortXmlNodesPublic(doc.Root);

        // Assert
        var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "zebra" }, elements);

        var zebraAttrs = doc.Root.Element("zebra")!.Attributes().Select(a => a.Name.LocalName).ToList();
        Assert.Equal(new[] { "attr1", "attr2" }, zebraAttrs);

        var zebraChildren = doc.Root.Element("zebra")!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "nested1", "nested2" }, zebraChildren);

        Assert.Equal("Text", doc.Root.Element("alpha")!.Value);
    }
}

// Testable wrapper class that exposes internal methods for testing
public class TestableProgram : Program
{
    public void SortXmlNodesPublic(XElement? element)
    {
        typeof(Program)
            .GetMethod("SortXmlNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(this, new object?[] { element });
    }

    public bool ProcessXmlFilePublic(string filePath)
    {
        return (bool)typeof(Program)
            .GetMethod("ProcessXmlFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(this, new object[] { filePath })!;
    }

    public List<string> FindFilesPublic(string pattern, bool recursive)
    {
        return (List<string>)typeof(Program)
            .GetMethod("FindFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(this, new object[] { pattern, recursive })!;
    }
}