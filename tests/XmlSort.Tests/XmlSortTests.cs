using System.Globalization;
using System.Threading;
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
            var result = Program.Main(new[] { Path.Combine(tempDir, "*.xml"), "--no-backup" });

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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            
            var testFile1 = Path.Combine(tempDir, "test1.xml");
            var testFile2 = Path.Combine(subDir, "test2.xml");
            
            File.WriteAllText(testFile1, "<root><z /><a /></root>");
            File.WriteAllText(testFile2, "<root><z /><a /></root>");

            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.xml", "-r", "--no-backup" });

            // Assert
            Assert.Equal(0, result);
            
            // Verify both files were sorted
            var doc1 = XDocument.Load(testFile1);
            var doc2 = XDocument.Load(testFile2);
            
            Assert.Equal("a", doc1.Root!.Elements().First().Name.LocalName);
            Assert.Equal("a", doc2.Root!.Elements().First().Name.LocalName);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.nonexistent" });

            // Assert
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            
            var testFile = Path.Combine(subDir, "test.xml");
            File.WriteAllText(testFile, "<root><z /><a /></root>");

            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = Program.Main(new[] { "*.xml", "--recursive", "--no-backup" });

            // Assert
            Assert.Equal(0, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
        program.SortXmlNodes(doc.Root);

        // Assert
        var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "beta", "zebra" }, elements);
    }

    [Fact]
    public void SortXmlNodes_SortsAttributesByName_WhenSortAttributesEnabled()
    {
        // Arrange
        var xml = @"<root zebra=""z"" alpha=""a"" beta=""b"" />";
        var doc = XDocument.Parse(xml);

        // Act – attribute sorting requires the --sort-attributes option
        var program = new TestableProgram { SortAttributes = true };
        program.SortXmlNodes(doc.Root);

        // Assert
        var attributes = doc.Root!.Attributes().Select(a => a.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "beta", "zebra" }, attributes);
    }

    [Fact]
    public void SortXmlNodes_PreservesAttributeOrder_ByDefault()
    {
        // Arrange – attributes are in reverse order
        var xml = @"<root zebra=""z"" alpha=""a"" beta=""b"" />";
        var doc = XDocument.Parse(xml);

        // Act – default behavior: no attribute sorting
        var program = new TestableProgram();
        program.SortXmlNodes(doc.Root);

        // Assert – original attribute order is preserved
        var attributes = doc.Root!.Attributes().Select(a => a.Name.LocalName).ToList();
        Assert.Equal(new[] { "zebra", "alpha", "beta" }, attributes);
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
        program.SortXmlNodes(doc.Root);

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
            var program = new TestableProgram { NoBackup = true };
            var result = program.ProcessXmlFile(testFile);

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
            var result = program.ProcessXmlFile(testFile);

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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test1.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test2.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "text");

            var program = new TestableProgram();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var files = program.FindFiles("*.xml", false);

            // Assert
            Assert.Equal(2, files.Count);
            Assert.All(files, f => Assert.EndsWith(".xml", f));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
            var files = program.FindFiles(Path.Combine(tempDir, "*.xml"), false);

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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);
            
            var testFile = Path.Combine(subDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            Directory.SetCurrentDirectory(tempDir);

            var program = new TestableProgram();

            // Act
            var files = program.FindFiles("subdir/*.xml", false);

            // Assert
            Assert.Single(files);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
        var files = program.FindFiles(nonExistentPath, false);

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
            var result = program.ProcessXmlFile(testFile);

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
            var result = Program.Main(new[] { Path.Combine(tempDir, "*.xml"), "--no-backup" });

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
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(tempDir, "test1.xml"), "<root />");
            File.WriteAllText(Path.Combine(subDir, "test2.xml"), "<root />");

            var program = new TestableProgram();
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var filesNonRecursive = program.FindFiles("*.xml", false);
            var filesRecursive = program.FindFiles("*.xml", true);

            // Assert
            Assert.Single(filesNonRecursive);
            Assert.Equal(2, filesRecursive.Count);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
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
        program.SortXmlNodes(doc.Root);

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

        // Act – with --sort-attributes to also sort attribute order
        var program = new TestableProgram { SortAttributes = true };
        program.SortXmlNodes(doc.Root);

        // Assert
        var elements = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "alpha", "zebra" }, elements);

        var zebraAttrs = doc.Root.Element("zebra")!.Attributes().Select(a => a.Name.LocalName).ToList();
        Assert.Equal(new[] { "attr1", "attr2" }, zebraAttrs);

        var zebraChildren = doc.Root.Element("zebra")!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "nested1", "nested2" }, zebraChildren);

        Assert.Equal("Text", doc.Root.Element("alpha")!.Value);
    }

    // -------------------------------------------------------------------------
    // IsXmlFile tests
    // -------------------------------------------------------------------------

    [Fact]
    public void IsXmlFile_WithValidXml_ReturnsTrue()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><child /></root>");

            var program = new TestableProgram();
            Assert.True(program.IsXmlFile(testFile));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsXmlFile_WithXmlDeclaration_ReturnsTrue()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<?xml version=\"1.0\" encoding=\"utf-8\"?><root />");

            var program = new TestableProgram();
            Assert.True(program.IsXmlFile(testFile));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsXmlFile_WithPlainText_ReturnsFalse()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "This is not XML at all.");

            var program = new TestableProgram();
            Assert.False(program.IsXmlFile(testFile));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsXmlFile_WithEmptyFile_ReturnsFalse()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "empty.xml");
            File.WriteAllText(testFile, string.Empty);

            var program = new TestableProgram();
            Assert.False(program.IsXmlFile(testFile));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -------------------------------------------------------------------------
    // CreateBackup tests
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateBackup_CreatesBackupWithExpectedName()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            var program = new TestableProgram { NoBackup = false };
            var backupPath = program.CreateBackup(testFile);

            Assert.NotNull(backupPath);
            Assert.Equal(Path.Combine(tempDir, "test-backup.xml"), backupPath);
            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_WithExistingBackup_CreatesNumberedBackup()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            // Pre-create first backup
            File.WriteAllText(Path.Combine(tempDir, "test-backup.xml"), "<root />");

            var program = new TestableProgram { NoBackup = false };
            var backupPath = program.CreateBackup(testFile);

            Assert.NotNull(backupPath);
            Assert.Equal(Path.Combine(tempDir, "test-backup2.xml"), backupPath);
            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_WithMultipleExistingBackups_UsesNextAvailableNumber()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test-backup.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test-backup2.xml"), "<root />");
            File.WriteAllText(Path.Combine(tempDir, "test-backup3.xml"), "<root />");

            var program = new TestableProgram { NoBackup = false };
            var backupPath = program.CreateBackup(testFile);

            Assert.NotNull(backupPath);
            Assert.Equal(Path.Combine(tempDir, "test-backup4.xml"), backupPath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_WithNoBackupOption_ReturnsNull()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root />");

            var program = new TestableProgram { NoBackup = true };
            var backupPath = program.CreateBackup(testFile);

            Assert.Null(backupPath);
            Assert.False(File.Exists(Path.Combine(tempDir, "test-backup.xml")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_FileWithoutExtension_CreatesCorrectBackupName()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "Makefile");
            File.WriteAllText(testFile, "<root />");

            var program = new TestableProgram { NoBackup = false };
            var backupPath = program.CreateBackup(testFile);

            Assert.NotNull(backupPath);
            Assert.Equal(Path.Combine(tempDir, "Makefile-backup"), backupPath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -------------------------------------------------------------------------
    // ProcessXmlFile backup behavior tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ProcessXmlFile_CreatesBackupByDefault()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><z /><a /></root>");

            var program = new TestableProgram();  // NoBackup = false by default
            program.ProcessXmlFile(testFile);

            Assert.True(File.Exists(Path.Combine(tempDir, "test-backup.xml")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ProcessXmlFile_WithNoBackup_DoesNotCreateBackup()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><z /><a /></root>");

            var program = new TestableProgram { NoBackup = true };
            program.ProcessXmlFile(testFile);

            Assert.False(File.Exists(Path.Combine(tempDir, "test-backup.xml")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -------------------------------------------------------------------------
    // Sorting option tests
    // -------------------------------------------------------------------------

    [Fact]
    public void SortXmlNodes_DefaultIsCaseInsensitive()
    {
        // Arrange – with case-insensitive sorting, "Beta" and "beta" are treated as equal
        // and sorted relative to "alpha" and "Zebra" without regard to letter casing.
        var xml = @"<root><Zebra /><alpha /><Beta /></root>";
        var doc = XDocument.Parse(xml);

        var program = new TestableProgram();  // CaseSensitive = false
        program.SortXmlNodes(doc.Root);

        // With case-insensitive sort, "alpha" must come first (before "Beta" and "Zebra")
        var names = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(3, names.Count);
        Assert.Equal("alpha", names[0]);
    }

    [Fact]
    public void SortXmlNodes_WithCaseSensitive_SortsUppercaseFirst()
    {
        // With CaseSensitive=true the sort uses StringComparison.CurrentCulture.
        // Pin to en-US so the assertion is not culture-dependent.
        var savedCulture = CultureInfo.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var xml = @"<root><beta /><Alpha /><zebra /></root>";
            var doc = XDocument.Parse(xml);

            var program = new TestableProgram { CaseSensitive = true };
            program.SortXmlNodes(doc.Root);

            // In en-US with case-sensitive sort, 'A' collates before 'b' and 'z'
            var names = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
            Assert.Equal(3, names.Count);
            var alphaIndex = names.IndexOf("Alpha");
            var betaIndex = names.IndexOf("beta");
            Assert.True(alphaIndex < betaIndex);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = savedCulture;
        }
    }

    // -------------------------------------------------------------------------
    // Duplicate detection tests
    // -------------------------------------------------------------------------

    [Fact]
    public void DetectAndHandleDuplicates_NoDuplicates_LeavesListUnchanged()
    {
        var xml = @"<root><a /><b /><c /></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram();
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(3, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithDuplicates_EmitsWarningButKeepsByDefault()
    {
        var xml = @"<root><a /><b /><a /></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = false };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        // Default: warn but do NOT remove
        Assert.Equal(3, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithRemoveDupes_RemovesDuplicates()
    {
        var xml = @"<root><a /><b /><a /></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        // Should keep only one <a>
        Assert.Equal(2, elements.Count);
        Assert.Equal(1, elements.Count(e => e.Name.LocalName == "a"));
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithRemoveDupes_NeverRemovesPropertyGroup()
    {
        // Use identical subtrees so the new subtree-equality check flags them as duplicates,
        // then verify the protected-names guard prevents removal.
        var xml = @"<Project><PropertyGroup><Foo /></PropertyGroup><PropertyGroup><Foo /></PropertyGroup></Project>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        // PropertyGroup must never be removed
        Assert.Equal(2, elements.Count(e => e.Name.LocalName == "PropertyGroup"));
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithRemoveDupes_NeverRemovesItemGroup()
    {
        // Use identical subtrees so the new subtree-equality check flags them as duplicates,
        // then verify the protected-names guard prevents removal.
        var xml = @"<Project><ItemGroup><Foo /></ItemGroup><ItemGroup><Foo /></ItemGroup></Project>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        // ItemGroup must never be removed
        Assert.Equal(2, elements.Count(e => e.Name.LocalName == "ItemGroup"));
    }

    [Fact]
    public void DetectAndHandleDuplicates_DuplicateWithSameAttributes_DetectedCaseInsensitive()
    {
        // <Foo a="1" /> and <foo A="1" /> are duplicates when case-insensitive
        var xml = @"<root><Foo a=""1"" /><foo A=""1"" /></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(1, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_DuplicateWithSameAttributes_NotDetectedCaseSensitive()
    {
        // <Foo a="1" /> and <foo a="1" /> are NOT duplicates when case-sensitive
        var xml = @"<root><Foo a=""1"" /><foo a=""1"" /></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCulture, elements);

        Assert.Equal(2, elements.Count);
    }

    [Fact]
    public void SortXmlNodes_WithRemoveDupes_RemovesDuplicatesFromOutput()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var testFile = Path.Combine(tempDir, "test.xml");
            File.WriteAllText(testFile, "<root><b /><a /><b /></root>");

            var program = new TestableProgram { NoBackup = true, RemoveDupes = true };
            var result = program.ProcessXmlFile(testFile);

            Assert.True(result);
            var doc = XDocument.Load(testFile);
            var names = doc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
            // Only one <b> should remain
            Assert.Equal(1, names.Count(n => n == "b"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithRemoveDupes_NeverRemovesPropertyGroupWithAttributes()
    {
        // PropertyGroup elements with identical subtrees AND attributes must still be protected
        var xml = @"<Project><PropertyGroup Condition="" '$(Config)'=='Release' ""><Foo /></PropertyGroup><PropertyGroup Condition="" '$(Config)'=='Release' ""><Foo /></PropertyGroup></Project>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(2, elements.Count(e => e.Name.LocalName == "PropertyGroup"));
    }

    [Fact]
    public void DetectAndHandleDuplicates_WithRemoveDupes_NeverRemovesItemGroupWithAttributes()
    {
        // ItemGroup elements with identical subtrees AND attributes must still be protected
        var xml = @"<Project><ItemGroup Condition=""x""><Foo /></ItemGroup><ItemGroup Condition=""x""><Foo /></ItemGroup></Project>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(2, elements.Count(e => e.Name.LocalName == "ItemGroup"));
    }

    [Fact]
    public void DetectAndHandleDuplicates_SameNameDifferentDescendants_NotDuplicates()
    {
        // Same element name and same direct attributes, but different child elements → NOT duplicates
        var xml = @"<root><a><child1 /></a><a><child2 /></a></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        // Both <a> elements must be kept because their subtrees differ
        Assert.Equal(2, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_IdenticalSubtrees_DetectedAsDuplicates()
    {
        // Two elements with identical subtrees (same name, attrs, and all descendants) are duplicates
        var xml = @"<root>
    <item id=""1""><sub val=""x"">text</sub></item>
    <item id=""1""><sub val=""x"">text</sub></item>
</root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(1, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_SameNameSameAttrsDifferentDescendantAttrValues_NotDuplicates()
    {
        // Descendant attribute values differ → NOT duplicates
        var xml = @"<root><a><b val=""1"" /></a><a><b val=""2"" /></a></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(2, elements.Count);
    }

    [Fact]
    public void DetectAndHandleDuplicates_SameNameSameAttrsDifferentTextContent_NotDuplicates()
    {
        // Same element name and attrs but different text content → NOT duplicates
        var xml = @"<root><a>hello</a><a>world</a></root>";
        var doc = XDocument.Parse(xml);
        var elements = doc.Root!.Elements().ToList();

        var program = new TestableProgram { RemoveDupes = true };
        program.DetectAndHandleDuplicates(doc.Root!, StringComparison.CurrentCultureIgnoreCase, elements);

        Assert.Equal(2, elements.Count);
    }
}

// Test wrapper class - methods are now internal and directly accessible via InternalsVisibleTo
public class TestableProgram : Program
{
}