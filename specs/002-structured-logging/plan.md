# Implementation Plan: Structured Diagnostic Logging

**Branch**: `002-structured-logging` | **Date**: 2026-02-06 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/002-structured-logging/spec.md`

## Summary

Add a centralized structured logger used by every component, writing JSON Lines logs to `%TEMP%\SqlEssentials.<PID>.debug.log`. Logging is controlled by `SQL_ESSENTIALS_LOG_LEVEL`, supports bounded async writes with rotation, and includes correlation IDs for request tracing. The core logging interfaces live in `SqlEssentials.Core`, while the file sink and VS package lifecycle integration live in `SqlEssentials.Extension`.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8  
**Primary Dependencies**: ScriptDOM, SMO, VS SDK 17.x, Community.VisualStudio.Toolkit  
**Storage**: Log files in `%TEMP%` (JSON Lines)  
**Testing**: xUnit + Moq (unit), VS SDK test host (integration where needed)  
**Target Platform**: Windows, SSMS 18+ (primary: SSMS 21/22 64-bit)  
**Project Type**: Single solution with Core library + Extension host  
**Performance Goals**: ≤1 microsecond overhead when logging is off; ≤2ms per request at Trace  
**Constraints**: Non-blocking UI, bounded queue with drop-on-full, rotate at 10MB default  
**Scale/Scope**: Multiple VS instances per machine; high-frequency completions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Status | Evidence |
|-----------|-------------|--------|----------|
| **I. Async-First / Non-Blocking UI** | All I/O and logging off UI thread | ✅ PASS | Bounded background writer (research.md) |
| **II. Core Engine Separation** | Core logging interfaces independent of VS | ✅ PASS | Logger contracts in Core, file sink in Extension (contracts/, data-model.md) |
| **III. ScriptDOM + SMO Foundation** | Parsing/metadata on official libs | ⬜ N/A | Logging feature only |
| **IV. Performance-First** | ≤100ms autocomplete target preserved | ✅ PASS | SC-001/SC-002, bounded queue | 
| **V. Profile-Driven Configuration** | JSON profile settings | ⬜ N/A | Logging configuration is env var-based |
| **VI. Privacy-First AI** | AI opt-in only | ⬜ N/A | No AI processing |
| **VII. Previewable & Undoable Transformations** | Diff preview for refactors | ⬜ N/A | No transformations |

**Gate Status**: ✅ ALL APPLICABLE PRINCIPLES SATISFIED

## Project Structure

### Documentation (this feature)

```text
specs/002-structured-logging/
├── plan.md              # This file
├── research.md          # Phase 0 - technology decisions
├── data-model.md        # Phase 1 - entity definitions
├── quickstart.md        # Phase 1 - dev usage guide
├── contracts/           # Phase 1 - interface contracts
│   ├── ILogger.cs
│   ├── ILogScope.cs
│   └── LogEntry.cs
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
SqlEssentials.sln
src/
├── SqlEssentials.Core/
│   ├── Completion/
│   ├── Context/
│   ├── Metadata/
│   ├── Models/
│   └── Logging/           # New: logger contracts, scopes, log entry models
└── SqlEssentials.Extension/
    ├── Commands/
    ├── Completion/
    ├── Properties/
    └── Logging/           # New: file sink, rotation, package lifecycle hooks

tests/
└── SqlEssentials.Core.Tests/
```

**Structure Decision**: Single solution with Core library for portable logging contracts and Extension host for file I/O and VS lifecycle integration.

## Constitution Check (Post-Design)

| Principle | Status | Evidence |
|-----------|--------|----------|
| **I. Async-First / Non-Blocking UI** | ✅ PASS | Background writer + drop-on-full (research.md) |
| **II. Core Engine Separation** | ✅ PASS | Contracts in [specs/002-structured-logging/contracts](./contracts) |
| **IV. Performance-First** | ✅ PASS | Guard-check fast path, bounded queue (data-model.md) |

**Gate Status**: ✅ ALL APPLICABLE PRINCIPLES SATISFIED

## Complexity Tracking

> No violations to justify — all Constitution principles satisfied.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *None* | — | — |
