<!-- Sync Impact Report
Version change: N/A → 1.0.0 (Initial)
Added sections: Core Principles (7), Technology Constraints, Development Workflow, Governance
Removed sections: None
Templates requiring updates: ✅ plan-template.md (Constitution Check section aligns)
Follow-up TODOs: None
-->

# SQL Essentials Constitution

## Core Principles

### I. Async-First / Non-Blocking UI

All operations that touch metadata retrieval, complex parsing, AI processing, or any I/O MUST execute asynchronously on background threads. The SSMS UI thread MUST never be blocked.

**Rationale**: SSMS users expect instant responsiveness; a hung UI destroys trust and productivity.

### II. Core Engine Separation

The core autocomplete, parsing, formatting, analysis, and refactoring engines MUST be implemented as standalone libraries (`SqlEssentials.Core`) that are independently testable without SSMS. The extension host (`SqlEssentials.Extension`) acts only as a thin integration layer.

**Rationale**: Enables comprehensive unit testing, CLI tooling, and future IDE portability.

### III. ScriptDOM + SMO Foundation

All T-SQL parsing and AST manipulation MUST use `Microsoft.SqlServer.TransactSql.ScriptDom`. All schema metadata access MUST use `Microsoft.SqlServer.Management.Smo` (SMO) with async wrappers.

**Rationale**: Official Microsoft libraries ensure correctness, dialect coverage, and forward compatibility.

### IV. Performance-First

- Autocomplete popup MUST appear within **≤100ms** of trigger keystroke (target: 50ms).
- Metadata cache MUST use TTL-based refresh (default: 30 minutes) with manual refresh option.
- Large schemas (10,000+ objects) MUST use deferred/lazy loading.
- Per-database cache memory SHOULD remain under 50MB for typical schemas.

**Rationale**: Competitive parity with SQL Prompt and dbForge requires sub-100ms response times.

### V. Profile-Driven Configuration

Formatting styles and analyzer rules MUST be configurable via named profiles stored as JSON files. Profiles MUST be exportable and shareable across teams. The tool MUST ship with at least one "sane default" profile.

**Rationale**: Team consistency and enterprise adoption require shareable, version-controlled settings.

### VI. Privacy-First AI (Optional)

All AI-powered features (Natural Language SQL, Explain SQL, AI completions) MUST be:
- Disabled by default.
- Opt-in per feature with explicit user consent.
- Transparent about data sent (with redaction/schema-only controls).
- Clearly labeled in the UI as AI-generated.

**Rationale**: Enterprise users require control and transparency over data leaving their environment.

### VII. Previewable & Undoable Transformations

All refactoring operations (rename, expand wildcards, qualify names) and formatting MUST:
- Present changes as a diff preview before applying.
- Apply as a single atomic undo unit.
- Never modify code without user confirmation.

**Rationale**: Prevents accidental corruption; aligns with safe refactoring practices.

## Technology Constraints

| Aspect | Requirement |
|--------|-------------|
| **Target Framework** | .NET Framework 4.8 (SSMS/VS Shell compatibility) |
| **Architecture** | amd64 (64-bit), AnyCPU with "Prefer 32-bit" disabled |
| **Extension Model** | VSIX/VSPackage with MEF component discovery |
| **IDE** | Visual Studio 2022 with "Visual Studio extension development" workload |
| **SSMS Compatibility** | SSMS 2016+ (primary: SSMS 21/22 64-bit) |
| **SQL Server Compatibility** | SQL Server 2012+ |

## Development Workflow

1. **Incremental Milestones**: Features delivered in weekly vertical slices.
2. **MVP-First**: Each phase delivers a testable, usable feature before moving to next.
3. **Test-Driven**: Unit tests for ranking, join generation, rewrites; golden-file tests for formatting.
4. **Performance Benchmarks**: Completion latency measured against large schema fixtures.

## Governance

- This Constitution supersedes conflicting guidance in other documents.
- Amendments require: documented rationale, version bump, and migration plan if breaking.
- All PRs/reviews MUST verify compliance with these principles.
- Complexity beyond these principles MUST be justified in writing.

**Version**: 1.0.0 | **Ratified**: 2026-02-04 | **Last Amended**: 2026-02-04
