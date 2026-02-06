# Implementation Plan: SQL Essentials MVP — Smart Autocomplete & Productivity Core

**Branch**: `1-smart-autocomplete` | **Date**: 2026-02-04 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/1-smart-autocomplete/spec.md`

## Summary

Build an SSMS plugin providing smart, context-aware T-SQL autocomplete and JOIN assistance. The implementation uses **AsyncPackage** for non-blocking extension loading, **IAsyncCompletionSource** for performant suggestions, **ScriptDOM** for T-SQL parsing, and **SMO** with async wrappers for schema metadata. Per-database in-memory caching with TTL ensures sub-100ms response times.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8  
**Primary Dependencies**: ScriptDOM (T-SQL parsing), SMO (metadata), VS SDK 17.x, Community.VisualStudio.Toolkit  
**Storage**: JSON files in `%APPDATA%\SqlEssentials\` (settings, custom snippets)  
**Testing**: xUnit + Moq for unit tests, VS SDK test host for integration, golden file tests for formatter  
**Target Platform**: SSMS 18+ (VS Shell), Primary: SSMS 21/22 64-bit  
**Project Type**: Single solution with Core library + Extension host  
**Performance Goals**: ≤100ms autocomplete latency (target: 50ms), 2s schema load for 500 tables  
**Constraints**: <50MB memory per database cache, non-blocking UI thread, no unhandled exceptions  
**Scale/Scope**: Databases with 1000+ tables, 10,000+ columns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Status | Evidence |
|-----------|-------------|--------|----------|
| **I. Async-First / Non-Blocking UI** | All metadata retrieval and parsing on background threads | ✅ PASS | AsyncPackage, IAsyncCompletionSource, Task.Run for SMO (research.md §2, §4) |
| **II. Core Engine Separation** | Core library independently testable without SSMS | ✅ PASS | SqlEssentials.Core + SqlEssentials.Extension separation (quickstart.md) |
| **III. ScriptDOM + SMO Foundation** | Use official MS libraries for parsing and metadata | ✅ PASS | TSql160Parser + SMO with async wrappers (research.md §3, §4) |
| **IV. Performance-First** | ≤100ms autocomplete, TTL cache, <50MB memory | ✅ PASS | SC-001: 100ms target, FR-015: 30min TTL, SC-005: 50MB limit |
| **V. Profile-Driven Configuration** | Formatting profiles as shareable JSON | ✅ PASS | FormattingProfile in data-model.md, JSON format in research.md §8 |
| **VI. Privacy-First AI** | AI features opt-in with consent | ⬜ N/A | AI features out of scope for MVP (Phase 5) |
| **VII. Previewable & Undoable Transformations** | Formatting with atomic undo | ✅ PASS | FR-023: preserve semantic meaning (refactoring is Phase 3) |

**Gate Status**: ✅ ALL APPLICABLE PRINCIPLES SATISFIED

## Project Structure

### Documentation (this feature)

```text
specs/1-smart-autocomplete/
├── plan.md              # This file
├── research.md          # Phase 0 - technology decisions
├── data-model.md        # Phase 1 - entity definitions
├── quickstart.md        # Phase 1 - dev environment setup
├── contracts/           # Phase 1 - interface contracts
│   ├── ICompletionProvider.cs
│   ├── IContextAnalyzer.cs
│   ├── IFormatter.cs
│   ├── ISchemaCache.cs
│   └── ISnippetManager.cs
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
sql-essentials/
├── SqlEssentials.sln
├── src/
│   ├── SqlEssentials.Core/           # Core engine (independently testable)
│   │   ├── Completion/               # ICompletionEngine, suggestion ranking
│   │   ├── Context/                  # IContextAnalyzer, clause detection
│   │   ├── Formatting/               # ISqlFormatter, visitor-based formatting
│   │   ├── Logging/                  # ILogger, LogLevel, LogScope (cross-cutting)
│   │   ├── Metadata/                 # ISchemaCache, SMO wrappers
│   │   ├── Snippets/                 # ISnippetManager, JSON loader
│   │   └── Models/                   # Domain entities from data-model.md
│   └── SqlEssentials.Extension/      # SSMS/VS integration layer
│       ├── Completion/               # IAsyncCompletionSource adapter
│       ├── Commands/                 # Format Document, Refresh Schema
│       ├── Logging/                  # FileLogger implementation, log file management
│       ├── Options/                  # Settings pages
│       └── source.extension.vsixmanifest
├── tests/
│   ├── SqlEssentials.Core.Tests/     # Unit tests (xUnit)
│   └── SqlEssentials.Integration.Tests/  # VS SDK test host
└── specs/
    └── 1-smart-autocomplete/
```

**Structure Decision**: Single solution with separation between Core library (portable, testable) and Extension host (thin VS/SSMS integration). Follows Constitution Principle II.

## Cross-Cutting: Structured Diagnostic Logging

All components emit structured log entries through a centralized `ILogger` interface (see `specs/002-structured-logging/spec.md`). Logging is controlled via the `SQL_ESSENTIALS_LOG_LEVEL` environment variable (Off by default). The logger writes to `%TEMP%\SqlEssentials.<PID>.debug.log` with ISO 8601 timestamps, component names, log levels, and optional correlation IDs for request tracing.

Every phase in this plan must instrument its components with logging calls. The logger is introduced in Phase 2 (Foundational) and each subsequent phase adds instrumentation to its new components. Existing ad-hoc logging (`Debug.WriteLine`, `File.AppendAllText` to perf.log, `ActivityLog`) is replaced during Phase 2.

## Complexity Tracking

> No violations to justify — all Constitution principles satisfied.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *None* | — | — |
