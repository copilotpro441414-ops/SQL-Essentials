# Quickstart: SQL Essentials Development Environment

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-02-04  
**Target**: Windows 10+ with Visual Studio 2022

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Visual Studio 2022 | 17.8+ | IDE with VSIX development |
| .NET Framework | 4.8 | Target framework |
| SQL Server | 2016+ | Testing database |
| SSMS | 18+ (21/22 recommended) | Extension host for testing |
| Git | 2.40+ | Version control |

### Visual Studio Workloads

Install via Visual Studio Installer:

1. **Visual Studio extension development** — Required for VSIX projects
2. **.NET desktop development** — Required for .NET Framework 4.8
3. **Data storage and processing** — Includes SQL Server tooling

```powershell
# Command-line installation (optional)
vs_installer.exe modify --installPath "C:\Program Files\Microsoft Visual Studio\2022\Enterprise" `
    --add Microsoft.VisualStudio.Workload.VisualStudioExtension `
    --add Microsoft.VisualStudio.Workload.ManagedDesktop `
    --add Microsoft.VisualStudio.Workload.Data
```

---

## Initial Setup

### 1. Clone Repository

```powershell
git clone https://github.com/YOUR_ORG/sql-essentials.git
cd sql-essentials
git checkout 1-smart-autocomplete
```

### 2. Restore NuGet Packages

```powershell
dotnet restore SqlEssentials.sln
# Or via Visual Studio: Right-click solution → Restore NuGet Packages
```

### 3. Build Solution

```powershell
# Command line
msbuild SqlEssentials.sln /p:Configuration=Debug /p:Platform="Any CPU"

# Or via Visual Studio: Ctrl+Shift+B
```

### 4. Verify SSMS Installation

Ensure SSMS is installed and note the installation path:

```powershell
# Typical SSMS 21 path
$ssmsPath = "C:\Program Files (x86)\Microsoft SQL Server Management Studio 21\Common7\IDE\Ssms.exe"

# Verify
Test-Path $ssmsPath
```

---

## Project Structure

After cloning, the repository should have this structure:

```
sql-essentials/
├── SqlEssentials.sln
├── src/
│   ├── SqlEssentials.Core/
│   │   └── SqlEssentials.Core.csproj
│   └── SqlEssentials.Extension/
│       ├── SqlEssentials.Extension.csproj
│       └── source.extension.vsixmanifest
├── tests/
│   ├── SqlEssentials.Core.Tests/
│   └── SqlEssentials.Integration.Tests/
├── specs/
│   └── 1-smart-autocomplete/
└── Requirements/
```

---

## Running the Extension

### Debug in Experimental Instance

1. Set `SqlEssentials.Extension` as the startup project
2. Press **F5** to start debugging
3. Visual Studio launches an **Experimental Instance** of VS
4. Open SSMS from within the experimental instance or attach to existing SSMS

### Debug in SSMS Directly

For testing in actual SSMS:

1. Build the solution in **Release** mode
2. Copy the VSIX output to SSMS extensions folder:

```powershell
$vsixPath = ".\src\SqlEssentials.Extension\bin\Release\SqlEssentials.vsix"
$ssmsExtPath = "$env:LOCALAPPDATA\Microsoft\SQL Server Management Studio\21.0\Extensions"

# Install VSIX
Start-Process -FilePath $vsixPath -Wait
```

3. Launch SSMS and verify the extension loads

### Attach Debugger to SSMS

```powershell
# 1. Launch SSMS normally
# 2. In VS: Debug → Attach to Process → select "Ssms.exe"
# 3. Set breakpoints in SqlEssentials.Core or SqlEssentials.Extension
```

---

## Running Tests

### Unit Tests (SqlEssentials.Core.Tests)

```powershell
# Run all unit tests
dotnet test tests/SqlEssentials.Core.Tests/SqlEssentials.Core.Tests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ContextAnalyzerTests"
```

### Integration Tests

Integration tests require VS SDK test host:

```powershell
# Ensure VS experimental instance is closed
dotnet test tests/SqlEssentials.Integration.Tests/SqlEssentials.Integration.Tests.csproj
```

---

## Key Dependencies

### NuGet Packages

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| `Microsoft.SqlServer.TransactSql.ScriptDom` | 161.* | Core | T-SQL parsing |
| `Microsoft.SqlServer.SqlManagementObjects` | 170.* | Core | Schema metadata |
| `Microsoft.VisualStudio.SDK` | 17.* | Extension | VS extensibility |
| `Community.VisualStudio.Toolkit` | 17.* | Extension | VS helper APIs |
| `xunit` | 2.6.* | Tests | Test framework |
| `Moq` | 4.20.* | Tests | Mocking |

### Assembly References

These are typically auto-resolved but may need manual configuration:

```xml
<!-- SqlEssentials.Core.csproj -->
<Reference Include="Microsoft.SqlServer.TransactSql.ScriptDom">
  <HintPath>$(NuGetPackageRoot)microsoft.sqlserver.transactsql.scriptdom\161.0.0\lib\net462\Microsoft.SqlServer.TransactSql.ScriptDom.dll</HintPath>
</Reference>
```

---

## Configuration Files

### Extension Manifest

`src/SqlEssentials.Extension/source.extension.vsixmanifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="SqlEssentials" Version="1.0.0" Language="en-US" Publisher="Your Name" />
    <DisplayName>SQL Essentials</DisplayName>
    <Description>Smart T-SQL autocomplete and productivity tools for SSMS</Description>
    <Tags>SQL, SSMS, Autocomplete, IntelliSense</Tags>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Pro" Version="[17.0,18.0)" />
    <!-- SSMS targets will be added based on compatibility testing -->
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP" DisplayName=".NET Framework" Version="[4.8,)" />
  </Dependencies>
</PackageManifest>
```

### User Settings Location

Extension settings are stored in:

```
%APPDATA%\SqlEssentials\
├── settings.json        # General settings
├── snippets.json        # Custom snippets
└── profiles/
    └── default.json     # Default formatting profile
```

---

## Development Workflow

### Daily Development

1. Pull latest changes: `git pull origin 1-smart-autocomplete`
2. Build solution: `Ctrl+Shift+B`
3. Run unit tests: `dotnet test`
4. Debug in experimental instance: `F5`
5. Commit changes: `git commit -am "description"`

### Before PR

```powershell
# 1. Run all tests
dotnet test SqlEssentials.sln

# 2. Build Release
msbuild SqlEssentials.sln /p:Configuration=Release

# 3. Test VSIX installation
# (manually install and test in SSMS)

# 4. Update documentation if needed
```

---

## Troubleshooting

### "Could not load file or assembly 'Microsoft.SqlServer.TransactSql.ScriptDom'"

**Cause**: ScriptDOM DLL not found or version mismatch.

**Fix**: Ensure NuGet package is restored and binding redirects are configured:

```xml
<!-- App.config -->
<dependentAssembly>
  <assemblyIdentity name="Microsoft.SqlServer.TransactSql.ScriptDom" publicKeyToken="89845dcd8080cc91" />
  <bindingRedirect oldVersion="0.0.0.0-16.100.0.0" newVersion="16.100.0.0" />
</dependentAssembly>
```

### Extension Not Loading in SSMS

**Cause**: VSIX targeting wrong VS Shell version.

**Fix**: Verify `InstallationTarget` in manifest matches SSMS version:
- SSMS 18: VS 2017 Shell (15.0)
- SSMS 19-20: VS 2019 Shell (16.0)
- SSMS 21-22: VS 2022 Shell (17.0)

### Debugger Not Hitting Breakpoints

**Cause**: Debugging optimized/release build or symbols not loaded.

**Fix**:
1. Ensure Debug configuration
2. Check **Debug → Windows → Modules** for symbol status
3. Disable "Just My Code" in Debug options

### SMO Connection Timeout

**Cause**: Slow metadata queries on large databases.

**Fix**: Increase connection timeout in SMO:

```csharp
var conn = new ServerConnection(connectionString);
conn.StatementTimeout = 60; // seconds
```

---

## Useful Commands

```powershell
# Clean and rebuild
msbuild SqlEssentials.sln /t:Clean,Build /p:Configuration=Debug

# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report

# Package VSIX for distribution
msbuild src/SqlEssentials.Extension/SqlEssentials.Extension.csproj /t:Package /p:Configuration=Release
```

---

## Next Steps

After environment setup:

1. Review [spec.md](./spec.md) for feature requirements
2. Review [data-model.md](./data-model.md) for entity definitions
3. Review [contracts/](./contracts/) for interface contracts
4. Run `/speckit.tasks` to generate implementation tasks
