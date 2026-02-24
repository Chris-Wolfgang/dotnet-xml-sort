# XML Sort

A cross-platform .NET CLI tool that sorts XML files alphabetically by element and attribute names. Install it globally and invoke it as `dotnet sort-xml`.

## Features

- 🔍 **Pattern-based file matching** - Use wildcards like `*.csproj` or `*.xml`
- 📁 **Recursive directory search** - Optional `-r` or `--recursive` flag
- ✅ **XML sniffing** - Reads only the first 512 bytes to detect XML; non-XML files are skipped with a clear error
- 🔄 **Recursive sorting** - Sorts all nested elements at every level
- 💾 **Safe backups** - Creates `filename-backup.ext` (or `-backup2`, `-backup3` …) before overwriting
- 🔠 **Configurable case sensitivity** - Defaults to current-culture, case-insensitive; opt in with `-cs`
- 🏷️ **Optional attribute sorting** - Preserves attribute order by default; sort with `-sa`
- ⚠️ **Duplicate detection** - Warns in yellow on duplicate sibling nodes; optionally removes them with `--remove-dupes`
- 🎯 **Multi-framework support** - Targets .NET 5.0-10.0 and .NET Framework 4.6.2-4.8.1

## Installation

### As a .NET Global Tool

```bash
dotnet tool install --global dotnet-sort-xml
```

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
dotnet sort-xml "*.csproj"
```

### Recursive Search

Sort all XML files in the current directory and all subdirectories:

```bash
dotnet sort-xml "*.xml" --recursive
```

or using the short form:

```bash
dotnet sort-xml "*.xml" -r
```

### Skip Backup Creation

Process files without creating backups:

```bash
dotnet sort-xml "*.xml" --no-backup
```

### Case-Sensitive Sorting

Sort using current culture in a case-sensitive way (default is case-insensitive):

```bash
dotnet sort-xml "*.xml" --case-sensitive
# or
dotnet sort-xml "*.xml" -cs
```

### Sort Attributes

Also sort each element's attributes (default preserves attribute order):

```bash
dotnet sort-xml "*.xml" --sort-attributes
# or
dotnet sort-xml "*.xml" -sa
```

### Duplicate Removal

Remove duplicate sibling elements with the same name and attributes (except `PropertyGroup` and `ItemGroup`, which are never removed):

```bash
dotnet sort-xml "*.xml" --remove-dupes
```

### Command-Line Options

```
Usage: dotnet sort-xml [options] <FilePattern>

Arguments:
  FilePattern             File name or filter (e.g., *.csproj)

Options:
  -r|--recursive          Search subfolders recursively
  --no-backup             Do not create a backup before overwriting
  -cs|--case-sensitive    Sort using current culture case-sensitively (default: case-insensitive)
  -sa|--sort-attributes   Also sort each element's attributes (default: preserve order)
  --remove-dupes          Remove duplicate sibling elements (except PropertyGroup and ItemGroup)
  -?|-h|--help            Show help information
```

## Examples

### Sort a Single File

```bash
dotnet sort-xml "MyProject.csproj"
```

### Sort All XML Files Recursively With All Options

```bash
dotnet sort-xml "*.xml" -r -cs -sa --remove-dupes --no-backup
```

## How It Works

The application:

1. **Finds files** matching the specified pattern (optionally searching subdirectories)
2. **Sniffs** the first 512 bytes of each file to efficiently detect XML — skips non-XML files with a red error
3. **Loads** the XML document with line-info preserved for useful duplicate warnings
4. **Sorts** all XML elements alphabetically by name (case-insensitive by default)
5. **Optionally sorts** all XML attributes alphabetically by name (`-sa`)
6. **Detects duplicates** and warns in yellow; optionally removes them (`--remove-dupes`)
7. **Creates a backup** of the original file before overwriting (unless `--no-backup`)
8. **Writes** the sorted XML back to the original file

### Sorting Behavior

- **Elements** are sorted alphabetically by their local name (case-insensitive by default; use `-cs` for case-sensitive)
- **Attributes** preserve their original order by default (use `-sa` to sort them too)
- **Text content** is preserved in its original position
- **Nested elements** are sorted recursively at all levels
- Elements with the same name are further sorted by their attribute set for stable ordering

### Backup Behavior

Before overwriting a file, a backup is created:

| Backup file | When created |
|-------------|-------------|
| `file-backup.xml` | First run |
| `file-backup2.xml` | If `file-backup.xml` already exists |
| `file-backup3.xml` | If both above exist |
| … | Continues incrementing |

Use `--no-backup` to skip backup creation entirely.

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
