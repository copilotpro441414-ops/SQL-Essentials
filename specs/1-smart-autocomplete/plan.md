# Implementation Plan: SQL Essentials MVP — Smart Autocomplete & Productivity Core

**Branch**: `1-smart-autocomplete` | **Date**: 2026-02-04 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/1-smart-autocomplete/spec.md`

## Summary

Build an SSMS extension providing context-aware T-SQL autocomplete, smart JOIN assistance, snippet expansion, and basic SQL formatting. The core engine uses **ScriptDOM** for parsing and **SMO** for async schema metadata retrieval, with an in-memory TTL-based cache. The extension integrates via **VSIX/VSPackage** with MEF component discovery.

## Technical Context

**Language/Version**: C# 7.3 / .NET Framework 4.8  
**Primary Dependencies**: Microsoft.SqlServer.TransactSql.ScriptDom, Microsoft.SqlServer.Management.Smo, Microsoft.VisualStudio.Shell.15.0, Community.VisualStudio.Toolkit  
**Storage**: In-memory cache only (no persistent storage for MVP)  
**Testing**: xUnit with Microsoft.VSSDK.TestHostFramework for integration tests  
**Target Platform**: Windows 10+ with SSMS 2016+ (primary: SSMS 21/22 64-bit)  
**Project Type**: VSIX extension (solution with Core library + Extension host)  
**Performance Goals**: ≤100ms autocomplete popup, ≤2s metadata load for 500 tables  
**Constraints**: <50MB memory for schema cache, async-first (no UI blocking), .NET 4.8 compatibility  
**Scale/Scope**: Support databases with 1000+ tables, 10,000+ columns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Implementation |
|-----------|--------|----------------|
| **I. Async-First / Non-Blocking UI** | ✅ PASS | All metadata loading and parsing runs on background threads via `Task.Run` and async/await. Debounced input cancels in-flight requests. |
| **II. Core Engine Separation** | ✅ PASS | `SqlEssentials.Core` contains completion engine, parser utilities, cache, and formatter. `SqlEssentials.Extension` is thin VSIX host. |
| **III. ScriptDOM + SMO Foundation** | ✅ PASS | ScriptDOM for T-SQL parsing/AST; SMO for schema metadata with async wrappers. |
| **IV. Performance-First** | ✅ PASS | 100ms target in spec (SC-001); TTL cache (FR-015); lazy loading for large schemas. |
| **V. Profile-Driven Configuration** | ✅ PASS | Formatting profiles stored as JSON (FR-022); shipped with default profile. |
| **VI. Privacy-First AI** | N/A | No AI features in MVP Phase 1. |
| **VII. Previewable Transformations** | ✅ PASS | Formatting applies as single undo unit; refactoring deferred to Phase 3. |

**Gate Result**: ✅ ALL GATES PASS — Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/1-smart-autocomplete/
├── plan.md              # This file
├── research.md          # Phase 0: Tech decisions & best practices
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Dev environment setup
├── contracts/           # Phase 1: Internal interface contracts
│   ├── ICompletionProvider.cs
│   ├── ISchemaCache.cs
│   ├── IContextAnalyzer.cs
│   └── IFormatter.cs
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2: Implementation tasks (via /speckit.tasks)
```

### Source Code (repository root)

```text
SqlEssentials.sln
│
├── src/
│   ├── SqlEssentials.Core/           # Core engine library (testable without SSMS)
│   │   ├── Completion/               # Autocomplete engine
│   │   │   ├── CompletionEngine.cs
│   │   │   ├── SuggestionRanker.cs
│   │   │   └── Providers/            # Column, Table, Keyword, Snippet providers
│   │   ├── Context/                  # Query context analysis
│   │   │   ├── ContextAnalyzer.cs
│   │   │   └── AliasTracker.cs
│   │   ├── JoinAssistant/            # JOIN predicate suggestions
│   │   │   ├── JoinPredicateGenerator.cs
│   │   │   └── ForeignKeyMatcher.cs
│   │   ├── Metadata/                 # Schema cache & SMO wrappers
│   │   │   ├── SchemaCache.cs
│   │   │   ├── SmoMetadataProvider.cs
│   │   │   └── Models/               # TableDefinition, ColumnDefinition, etc.
│   │   ├── Formatting/               # SQL formatter
│   │   │   ├── SqlFormatter.cs
│   │   │   └── FormattingProfile.cs
│   │   └── Snippets/                 # Snippet engine
│   │       ├── SnippetManager.cs
│   │       └── SnippetExpander.cs
│   │
│   └── SqlEssentials.Extension/      # VSIX host (thin integration layer)
│       ├── SqlEssentialsPackage.cs   # VSPackage entry point
│       ├── Commands/                 # Menu commands (Format, Refresh, etc.)
│       ├── Completion/               # VS completion source adapter
│       ├── Options/                  # Settings pages
│       └── source.extension.vsixmanifest
│
└── tests/
    ├── SqlEssentials.Core.Tests/     # Unit tests for core engine
    │   ├── Completion/
    │   ├── Context/
    │   ├── JoinAssistant/
    │   ├── Metadata/
    │   └── Formatting/
    └── SqlEssentials.Integration.Tests/  # Integration tests with SSMS test host
```

**Structure Decision**: Two-project solution following Constitution Principle II (Core Engine Separation). `SqlEssentials.Core` is a .NET 4.8 class library with zero VS dependencies, enabling full unit test coverage. `SqlEssentials.Extension` is a VSIX project that references Core and provides VS Shell integration.

## Complexity Tracking

> No violations — design follows constitution principles with minimal complexity.
