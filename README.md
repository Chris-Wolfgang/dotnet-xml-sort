# XML Sort

A cross-platform .NET console application that sorts XML files alphabetically by element and attribute names.

## Features

- 🔍 **Pattern-based file matching** - Use wildcards like `*.csproj` or `*.xml`
- 📁 **Recursive directory search** - Optional `-r` or `--recursive` flag
- ✅ **XML validation** - Automatically skips non-XML files
- 🔄 **Recursive sorting** - Sorts all nested elements and attributes
- 💾 **In-place updates** - Writes sorted XML back to original files
- 📝 **Content preservation** - Maintains text content and structure
- 🎯 **Multi-framework support** - Targets .NET 5.0-10.0 and .NET Framework 4.6.2-4.8.1

## Installation

### From Source

```bash
git clone https://github.com/Chris-Wolfgang/dotnet-xml-sort.git
cd dotnet-xml-sort
dotnet build -c Release
```

## Usage

### Basic Usage

Sort all `.csproj` files in the current directory:

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "*.csproj"
```

### Recursive Search

Sort all XML files in the current directory and all subdirectories:

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "*.xml" --recursive
```

or using the short form:

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "*.xml" -r
```

### Command-Line Options

```
Usage: xmlsort [options] <FilePattern>

Arguments:
  FilePattern            File name or filter (e.g., *.csproj)

Options:
  -r|--recursive         Search subfolders recursively
  -?|-h|--help          Show help information
```

## Examples

### Sort a Single File

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "MyProject.csproj"
```

### Sort All XML Files

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "*.xml"
```

### Sort All Config Files Recursively

```bash
dotnet run --project src/XmlSort/XmlSort.csproj -- "*.config" -r
```

## How It Works

The application:

1. **Finds files** matching the specified pattern (optionally searching subdirectories)
2. **Validates** that each file contains valid XML
3. **Sorts** all XML elements alphabetically by name
4. **Sorts** all XML attributes alphabetically by name
5. **Preserves** text content within elements
6. **Writes** the sorted XML back to the original file

### Sorting Behavior

- **Elements** are sorted alphabetically by their local name
- **Attributes** are sorted alphabetically by their local name
- **Text content** is preserved in its original position
- **Nested elements** are sorted recursively at all levels
- Elements with the same name are further sorted by their attributes for consistency

### Example Transformation

**Before:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
    <PackageReference Include="AutoMapper" Version="12.0.0" />
  </ItemGroup>
</Project>
```

**After:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="12.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Development

### Building

```bash
dotnet restore
dotnet build --configuration Release
```

### Running Tests

```bash
dotnet test --configuration Release
```

### Test Coverage

The project maintains >90% code coverage. To generate a coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory "./TestResults"
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html"
```

## Dependencies

- [McMaster.Extensions.CommandLineUtils](https://www.nuget.org/packages/McMaster.Extensions.CommandLineUtils/) - Command-line parsing

## Requirements

- .NET 5.0 SDK or later (for building and running)
- .NET Framework 4.6.2 or later (for .NET Framework targets)

## License

This project is licensed under the Mozilla Public License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## Author

Chris Wolfgang ([@Chris-Wolfgang](https://github.com/Chris-Wolfgang))
