# SQL Essentials Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-04

## Active Technologies
- C# / .NET Framework 4.8 + ScriptDOM (T-SQL parsing), SMO (metadata), VS SDK 17.x, Community.VisualStudio.Toolkit (1-smart-autocomplete)
- JSON files in `%APPDATA%\SqlEssentials\` (settings, custom snippets) (1-smart-autocomplete)
- C# / .NET Framework 4.8 + ScriptDOM, SMO, VS SDK 17.x, Community.VisualStudio.Toolkit (002-structured-logging)
- Log files in `%TEMP%` (JSON Lines) (002-structured-logging)
- C# 12 on .NET Framework 4.8 + xUnit 2.6.*, Moq 4.20.*, Microsoft.NET.Test.Sdk 17.*, ScriptDOM, SMO wrappers (003-add-unit-tests)
- In-memory test fixtures and test doubles (no external storage) (003-add-unit-tests)

- C# 7.3 / .NET Framework 4.8 + Microsoft.SqlServer.TransactSql.ScriptDom, Microsoft.SqlServer.Management.Smo, Microsoft.VisualStudio.Shell.15.0, Community.VisualStudio.Toolkit (1-smart-autocomplete)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 7.3 / .NET Framework 4.8

## Code Style

C# 7.3 / .NET Framework 4.8: Follow standard conventions

## Recent Changes
- 003-add-unit-tests: Added C# 12 on .NET Framework 4.8 + xUnit 2.6.*, Moq 4.20.*, Microsoft.NET.Test.Sdk 17.*, ScriptDOM, SMO wrappers
- 002-structured-logging: Added C# / .NET Framework 4.8 + ScriptDOM, SMO, VS SDK 17.x, Community.VisualStudio.Toolkit


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
