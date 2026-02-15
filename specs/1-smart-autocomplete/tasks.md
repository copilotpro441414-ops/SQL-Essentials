# Tasks: SQL Essentials MVP — Smart Autocomplete & Productivity Core

**Input**: Design documents from `/specs/1-smart-autocomplete/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **Core library**: `src/SqlEssentials.Core/`
- **Extension host**: `src/SqlEssentials.Extension/`
- **Unit tests**: `tests/SqlEssentials.Core.Tests/`
- **Integration tests**: `tests/SqlEssentials.Integration.Tests/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Project initialization, solution structure, and build configuration

- [X] T001 Create solution file `SqlEssentials.sln` at repository root
- [X] T002 [P] Create `src/SqlEssentials.Core/SqlEssentials.Core.csproj` targeting .NET Framework 4.8
- [X] T003 [P] Create `src/SqlEssentials.Extension/SqlEssentials.Extension.csproj` as VSIX project
- [X] T004 [P] Create `tests/SqlEssentials.Core.Tests/SqlEssentials.Core.Tests.csproj` with xUnit
- [X] T005 Add NuGet packages to Core: Microsoft.SqlServer.TransactSql.ScriptDom, Microsoft.SqlServer.SqlManagementObjects
- [X] T006 [P] Add NuGet packages to Extension: Microsoft.VisualStudio.SDK, Community.VisualStudio.Toolkit.17
- [X] T007 [P] Add NuGet packages to Tests: xunit, xunit.runner.visualstudio, Moq
- [X] T008 Create `src/SqlEssentials.Extension/source.extension.vsixmanifest` with metadata
- [X] T009 [P] Configure `.editorconfig` for C# coding standards at repository root
- [X] T010 [P] Create `Directory.Build.props` for shared build settings

---

## Phase 2: Foundational (Shared Infrastructure)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can work. Includes schema caching (US5), shared completion models, and completion engine shell.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

**Independent Test**: Connect to a database, verify metadata loads in background, trigger autocomplete and confirm popup appears within 100ms (even if suggestions are basic).

### Metadata Models

- [X] T011 [P] Create `DatabaseConnection` model in `src/SqlEssentials.Core/Models/DatabaseConnection.cs`
- [X] T012 [P] Create `AuthenticationMethod` enum in `src/SqlEssentials.Core/Models/AuthenticationMethod.cs`
- [X] T013 [P] Create `SchemaCache` model in `src/SqlEssentials.Core/Models/SchemaCache.cs`
- [X] T014 [P] Create `CacheStatus` enum in `src/SqlEssentials.Core/Models/CacheStatus.cs`
- [X] T015 [P] Create `TableDefinition` model in `src/SqlEssentials.Core/Models/TableDefinition.cs`
- [X] T016 [P] Create `ColumnDefinition` model in `src/SqlEssentials.Core/Models/ColumnDefinition.cs`
- [X] T017 [P] Create `ForeignKeyRelationship` model in `src/SqlEssentials.Core/Models/ForeignKeyRelationship.cs`
- [X] T018 [P] Create `ProcedureDefinition` model in `src/SqlEssentials.Core/Models/ProcedureDefinition.cs`
- [X] T019 [P] Create `FunctionDefinition` model in `src/SqlEssentials.Core/Models/FunctionDefinition.cs`
- [X] T020 [P] Create `ParameterDefinition` model in `src/SqlEssentials.Core/Models/ParameterDefinition.cs`

### Completion Models (Shared)

- [X] T021 [P] Create `ClauseType` enum in `src/SqlEssentials.Core/Models/ClauseType.cs`
- [X] T022 [P] Create `TriggerReason` enum in `src/SqlEssentials.Core/Models/TriggerReason.cs`
- [X] T023 [P] Create `AliasBinding` model in `src/SqlEssentials.Core/Models/AliasBinding.cs`
- [X] T024 [P] Create `AutocompleteContext` model in `src/SqlEssentials.Core/Models/AutocompleteContext.cs`
- [X] T025 [P] Create `Suggestion` model in `src/SqlEssentials.Core/Models/Suggestion.cs`
- [X] T026 [P] Create `SuggestionType` enum in `src/SqlEssentials.Core/Models/SuggestionType.cs`
- [X] T027 [P] Create `TextSpan` model in `src/SqlEssentials.Core/Models/TextSpan.cs`

### Schema Cache Implementation

- [X] T028 Create `ISchemaCache` interface in `src/SqlEssentials.Core/Metadata/ISchemaCache.cs` (from contracts/)
- [X] T029 Create `IDatabaseCache` interface in `src/SqlEssentials.Core/Metadata/IDatabaseCache.cs`
- [X] T030 Create `DatabaseCache` implementation in `src/SqlEssentials.Core/Metadata/DatabaseCache.cs`
- [X] T031 Create `SmoMetadataLoader` async wrapper in `src/SqlEssentials.Core/Metadata/SmoMetadataLoader.cs`
- [X] T032 Create `SchemaCache` implementation in `src/SqlEssentials.Core/Metadata/SchemaCache.cs` (FR-012 to FR-015)
- [X] T033 Implement TTL-based cache expiration in `SchemaCache` (default 30 minutes per FR-015)
- [X] T034 Implement background refresh with stale-while-revalidate pattern in `SchemaCache`
- [X] T035 Create `CacheStatusChangedEventArgs` in `src/SqlEssentials.Core/Metadata/CacheStatusChangedEventArgs.cs`

### Context Analyzer Shell

- [X] T036 Create `IContextAnalyzer` interface in `src/SqlEssentials.Core/Context/IContextAnalyzer.cs` (from contracts/)
- [X] T037 Create `IAutocompleteContext` interface in `src/SqlEssentials.Core/Context/IAutocompleteContext.cs`
- [X] T038 Create `IAliasBinding` interface in `src/SqlEssentials.Core/Context/IAliasBinding.cs`
- [X] T039 Create `ContextAnalyzer` base implementation in `src/SqlEssentials.Core/Context/ContextAnalyzer.cs`
- [X] T040 Implement alias extraction using ScriptDOM visitor in `ContextAnalyzer` (FR-007)
- [X] T041 Implement `QualifierPrefix` detection for `alias.` pattern in `ContextAnalyzer`

### Completion Engine Shell

- [X] T042 Create `ICompletionEngine` interface in `src/SqlEssentials.Core/Completion/ICompletionEngine.cs` (from contracts/)
- [X] T043 Create `ICompletionResult` interface in `src/SqlEssentials.Core/Completion/ICompletionResult.cs`
- [X] T044 Create `ISuggestion` interface in `src/SqlEssentials.Core/Completion/ISuggestion.cs`
- [X] T045 Create `CompletionEngine` base implementation in `src/SqlEssentials.Core/Completion/CompletionEngine.cs`
- [X] T046 Implement basic suggestion generation (all tables, views, columns) in `CompletionEngine`

### Extension Integration (Completion Shell)

- [X] T047 Create `SqlEssentialsPackage` AsyncPackage in `src/SqlEssentials.Extension/SqlEssentialsPackage.cs`
- [X] T048 Implement connection event handling to trigger cache load in `SqlEssentialsPackage`
- [X] T049 Create `RefreshSchemaCommand` in `src/SqlEssentials.Extension/Commands/RefreshSchemaCommand.cs` (FR-014)
- [X] T050 Create `SqlCompletionSource` implementing `IAsyncCompletionSource` in `src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs`
- [X] T051 Create `SqlCompletionSourceProvider` with MEF export in `src/SqlEssentials.Extension/Completion/SqlCompletionSourceProvider.cs`
- [X] T052 Implement trigger detection for dot-after-identifier in `SqlCompletionSource` (FR-002)
- [X] T053 Implement suggestion filtering as user types in `SqlCompletionSource` (FR-003)
- [X] T054 Implement Tab/Enter selection and Escape dismissal in `SqlCompletionSource` (FR-004, FR-005)

### Structured Diagnostic Logging (Cross-Cutting Infrastructure)

**Purpose**: Centralized logging that replaces all ad-hoc Debug.WriteLine/perf.log/ActivityLog calls. Controlled via `SQL_ESSENTIALS_LOG_LEVEL` env var. See `specs/002-structured-logging/spec.md`.

- [X] T054d [P] Create `LogLevel` enum (Trace, Debug, Info, Warning, Error, Off) in `src/SqlEssentials.Core/Logging/LogLevel.cs`
- [X] T054e [P] Create `ILogger` interface in `src/SqlEssentials.Core/Logging/ILogger.cs` with methods: Trace, Debug, Info, Warning, Error, IsEnabled(level), BeginScope
- [X] T054f [P] Create `LogScope` disposable class in `src/SqlEssentials.Core/Logging/LogScope.cs` (tracks elapsed time + correlation ID)
- [X] T054g [P] Create `NullLogger` (no-op implementation) in `src/SqlEssentials.Core/Logging/NullLogger.cs`
- [X] T054h Create `FileLogger` implementation in `src/SqlEssentials.Extension/Logging/FileLogger.cs` — buffered async writes to `%TEMP%\SqlEssentials.<PID>.debug.log`, log rotation at 10 MB
- [X] T054i Create `LoggerFactory` in `src/SqlEssentials.Extension/Logging/LoggerFactory.cs` — reads `SQL_ESSENTIALS_LOG_LEVEL` env var, returns FileLogger or NullLogger
- [X] T054j Integrate logger into `SqlEssentialsPackage.InitializeAsync` — create logger, log package initialization, pass to all components
- [X] T054k Add ILogger parameter to `CompletionEngine` constructor, add logging at: method entry, context analysis result, cache lookup, suggestion count, elapsed time
- [X] T054l Add ILogger parameter to `ContextAnalyzer` constructor, add logging at: clause detection, alias extraction, partial input parsing
- [X] T054m Add ILogger parameter to `SchemaCache` constructor, add logging at: cache hit/miss, load start/complete, refresh start/complete, expiration, errors
- [X] T054n Add ILogger parameter to `SmoMetadataLoader`, add logging at: connection attempt, table/view/column counts loaded, elapsed time
- [X] T054o Add logging to `SqlCompletionSource`: trigger type, connection key, fallback items, elapsed time, item count
- [X] T054p Add logging to `RefreshSchemaCommand` and `LogContentTypeCommand`: command invoked, result
- [X] T054q Remove all existing ad-hoc logging: Debug.WriteLine calls, File.AppendAllText to perf.log, ActivityLog calls — replace with ILogger calls
- [X] T054r Add ILogger unit tests in `tests/SqlEssentials.Core.Tests/Logging/` — verify level filtering, NullLogger no-op, LogScope elapsed time
- [X] T054s Implement `IDisposable` on `FileLogger` for clean flush/close; wire to package Dispose

**Checkpoint**: Foundational complete - schema cache loads, basic autocomplete popup works, all stories can now proceed independently

### ⏱️ Performance Validation (Post-Foundational)

- [ ] T054a **[PERF]** Validate autocomplete popup appears within 100ms after typing trigger (SC-001, Constitution IV)
- [ ] T054b **[PERF]** Verify completion response time stays <100ms during background cache refresh
- [ ] T054c **[PERF]** Test autocomplete latency with cold vs warm cache (target: 50ms warm)

> **STOP if T054a-c fail**: Performance is a Constitution principle. Do not proceed to user stories until autocomplete latency meets 100ms threshold.

---

## Phase 3: User Story 1 - Context-Aware Column Completion (Priority: P1) 🎯 MVP

**Goal**: After typing `alias.`, display only columns from that specific table within 100ms

**Independent Test**: Connect to any database, write `SELECT * FROM Users u`, type `u.` and verify only Users columns appear

### Implementation for User Story 1

- [X] T055 [US1] Implement column-only filtering when `alias.` detected in `CompletionEngine` (FR-008)
- [X] T056 [US1] Implement alias-to-table resolution using cached metadata in `CompletionEngine`
- [X] T057 [US1] Implement basic ranking: PK columns first, then alphabetical in `CompletionEngine`
- [X] T058 [US1] Add column data type display in suggestion tooltip

**Checkpoint**: User Story 1 fully functional - `alias.` shows correct columns within 100ms

### Logging Instrumentation for User Story 1

- [X] T058a [US1] Add Trace-level logging to column-only filter path in `CompletionEngine` — log alias resolved, table matched, column count returned

---

## Phase 4: User Story 2 - Clause-Aware Object Completion (Priority: P1)

**Goal**: Prioritize suggestions based on current clause context (FROM → tables, SELECT → columns, WHERE → columns/functions)

**Independent Test**: Position cursor in different clauses, trigger autocomplete, verify suggestion categories match clause

### Clause Detection for User Story 2

- [X] T059 [US2] Create `ClauseDetectionVisitor` in `src/SqlEssentials.Core/Context/ClauseDetectionVisitor.cs`
- [X] T060 [US2] Implement clause detection using ScriptDOM in `ContextAnalyzer.GetClauseAtPosition()` (FR-006)
- [X] T061 [US2] Add clause visitor for SELECT, FROM, WHERE, JOIN, ON, GROUP BY, ORDER BY in `ContextAnalyzer`

### Clause-Aware Ranking for User Story 2

- [X] T062 [US2] Implement clause-based suggestion prioritization in `CompletionEngine` (FR-006)
- [X] T063 [US2] Add table/view prioritization for FROM and JOIN clauses in `CompletionEngine`
- [X] T064 [US2] Add column prioritization for SELECT and WHERE clauses in `CompletionEngine`
- [X] T065 [US2] Implement ranking algorithm per FR-024: prefix match, type relevance, key status in `CompletionEngine`

### Extension Enhancements for User Story 2

- [X] T066 [US2] Implement trigger detection for space-after-keyword in `SqlCompletionSource` (FR-002)
- [X] T067 [US2] Add Ctrl+Space explicit trigger support in `SqlCompletionSource` (FR-002)

**Checkpoint**: User Story 2 complete - suggestions prioritized by clause context

### Logging Instrumentation for User Story 2

- [X] T067d [US2] Add Trace-level logging to clause detection in `ClauseDetectionVisitor` — log detected clause type and cursor position
- [X] T067e [US2] Add Debug-level logging to clause-based ranking in `CompletionEngine` — log clause bonus applied per suggestion type

### ⏱️ Performance Validation (Post-MVP)

- [ ] T067a **[PERF]** Revalidate 100ms autocomplete latency with clause detection active
- [ ] T067b **[PERF]** Verify clause parsing overhead stays <20ms (clause detection should not impact completion time)
- [ ] T067c **[PERF]** 🎯 **MVP Gate**: Confirm MVP functionality meets all SC-001 latency requirements before expanding scope

> **MVP SHIPPABLE**: If T067a-c pass and US1+US2 functional, the extension is viable for early adopters.

---

## Phase 5: Unit Test Hardening (Pre-Phase-5 Insert)

**Goal**: Insert and execute a dedicated core-only unit-testing phase before JOIN/snippet/formatting phases.

**Independent Test**: Verify `specs/003-add-unit-tests/tasks.md` exists, is executable, and branch strategy is documented.

- [X] T067f Add pre-phase-5 testing phase reference linking to `specs/003-add-unit-tests/tasks.md`
- [X] T067g Update roadmap sequencing notes so this testing phase runs before JOIN work
- [X] T067h Add branch strategy note: implement on dedicated testing branch, merge back only after green tests

### Quality Gate Refinements (Phase 7 of 003-add-unit-tests)

- [X] T067i Differentiate per-module coverage thresholds (Completion/Context=85%, Metadata=75/70%, Logging=75/70%)
- [X] T067j Add mutation testing spot-check for SuggestionScoringPolicy (`stryker-config.json`, `scripts/quality/Run-MutationSpotCheck.ps1`)
- [X] T067k Add test runtime budget enforcement script (`scripts/quality/Verify-TestRuntime.ps1`, 90s gate)

---

## Phase 6: User Story 3 - Smart JOIN Predicate Suggestions (Priority: P2)

**Goal**: After `JOIN Orders o ON`, suggest `u.UserID = o.UserID` based on FK relationships

**Independent Test**: Connect to database with FKs, verify normal FK-based ON suggestions and odd/fallback scenarios (missing FK, unresolved aliases, multi-column keys).

### Models for User Story 3

- [X] T068 [P] [US3] Create `JoinMatchType` enum in `src/SqlEssentials.Core/Models/JoinMatchType.cs`
- [X] T069 [P] [US3] Create `JoinSuggestion` model in `src/SqlEssentials.Core/Models/JoinSuggestion.cs`
- [X] T070 [P] [US3] Create `IJoinContext` interface in `src/SqlEssentials.Core/Context/IJoinContext.cs`
- [X] T071 [P] [US3] Create `JoinContext` model in `src/SqlEssentials.Core/Models/JoinContext.cs`

### JOIN Predicate Generation

- [X] T072 [US3] Implement JOIN context detection in `ContextAnalyzer` (left table, right table identification)
- [X] T073 [US3] Create `JoinPredicateGenerator` in `src/SqlEssentials.Core/Completion/JoinPredicateGenerator.cs`
- [X] T074 [US3] Implement FK-based predicate suggestion in `JoinPredicateGenerator` (FR-009)
- [X] T075 [US3] Implement column-name-matching fallback in `JoinPredicateGenerator` (FR-010)
- [X] T076 [US3] Implement ranking: FK suggestions higher than name-based in `JoinPredicateGenerator` (FR-011)

### Integration for User Story 3

- [X] T077 [US3] Integrate `JoinPredicateGenerator` with `CompletionEngine` for ON clause context
- [X] T078 [US3] Add JOIN predicate as `SuggestionType.JoinPredicate` in completion results

**Checkpoint**: User Story 3 complete - JOIN ON suggestions based on FK/column matching

### Logging Instrumentation for User Story 3

- [X] T078a [US3] Add Debug-level logging to `JoinPredicateGenerator` — log FK lookup result, column name match fallback, predicate generated

### Test Coverage for User Story 3 (Normal + Odd Cases)

- [X] T078b [P] [US3] Add JOIN predicate happy-path tests (single FK, multi-column FK) in `tests/SqlEssentials.Core.Tests/Completion/JoinPredicateGeneratorTests.cs`
- [X] T078c [P] [US3] Add JOIN odd-case tests (no FK fallback, unresolved alias/table, duplicate predicate dedupe) in `tests/SqlEssentials.Core.Tests/Completion/JoinPredicateGeneratorTests.cs`
- [X] T078d [P] [US3] Add ON-clause join context detection tests (normal + malformed/partial SQL) in `tests/SqlEssentials.Core.Tests/Context/ContextAnalyzerJoinContextTests.cs`
- [X] T078e [US3] Run US3-focused tests and verify all normal/odd scenarios pass before phase sign-off, then run post-test coverage gate (`pwsh scripts/quality/Verify-CoreCoverage.ps1`)

---

## Phase 7: User Story 4 - Keyword and Snippet Completion (Priority: P2)

**Goal**: Suggest T-SQL keywords and expand snippet shortcuts with placeholder navigation

**Independent Test**: Type `SEL`/`selj` for normal flows and validate odd snippet/placeholder cases (invalid placeholders, missing defaults, partial shortcuts).

### Models for User Story 4

- [X] T079 [P] [US4] Create `Snippet` model in `src/SqlEssentials.Core/Models/Snippet.cs`
- [X] T080 [P] [US4] Create `PlaceholderDefinition` model in `src/SqlEssentials.Core/Models/PlaceholderDefinition.cs`
- [X] T081 [P] [US4] Create `SnippetExpansionSession` model in `src/SqlEssentials.Core/Models/SnippetExpansionSession.cs`
- [X] T082 [P] [US4] Create `PlaceholderSpan` model in `src/SqlEssentials.Core/Models/PlaceholderSpan.cs`

### Snippet Manager Implementation

- [X] T083 [US4] Create `ISnippetManager` interface in `src/SqlEssentials.Core/Snippets/ISnippetManager.cs` (from contracts/)
- [X] T084 [US4] Create `ISnippet` interface in `src/SqlEssentials.Core/Snippets/ISnippet.cs`
- [X] T085 [US4] Create `SnippetManager` implementation in `src/SqlEssentials.Core/Snippets/SnippetManager.cs`
- [X] T086 [US4] Create `BuiltInSnippets.json` resource in `src/SqlEssentials.Core/Snippets/Resources/BuiltInSnippets.json` (FR-016)
- [X] T087 [US4] Implement built-in snippets: SELECT, INSERT, UPDATE, DELETE, CTE, TRY/CATCH in JSON
- [X] T088 [US4] Implement snippet loading from embedded JSON resource in `SnippetManager`

### Keyword Provider

- [X] T089 [US4] Create `KeywordProvider` in `src/SqlEssentials.Core/Completion/KeywordProvider.cs`
- [X] T090 [US4] Add T-SQL keyword list (SELECT, FROM, WHERE, JOIN, etc.) to `KeywordProvider`
- [X] T091 [US4] Integrate `KeywordProvider` with `CompletionEngine`

### Snippet Expansion

- [X] T092 [US4] Create `SnippetExpander` in `src/SqlEssentials.Extension/Snippets/SnippetExpander.cs`
- [X] T093 [US4] Implement placeholder parsing (TextMate-style $1, ${2:default}) in `SnippetExpander`
- [X] T094 [US4] Implement Tab navigation between placeholders in `SnippetExpander` (FR-017)
- [X] T095 [US4] Integrate snippets as `SuggestionType.Snippet` in completion results

**Checkpoint**: User Story 4 complete - keywords and snippets with placeholder navigation

### Logging Instrumentation for User Story 4

- [X] T095a [US4] Add Debug-level logging to `SnippetManager` — log snippet load count, custom snippet add/update/remove
- [X] T095b [US4] Add Trace-level logging to `SnippetExpander` — log snippet expansion trigger, placeholder navigation

### Test Coverage for User Story 4 (Normal + Odd Cases)

- [X] T095c [P] [US4] Add keyword/snippet happy-path tests (shortcut match, snippet insertion, placeholder tab order) in `tests/SqlEssentials.Core.Tests/Completion/KeywordAndSnippetTests.cs`
- [X] T095d [P] [US4] Add odd-case tests (invalid placeholder syntax, missing snippet body, ambiguous shortcut collisions) in `tests/SqlEssentials.Core.Tests/Completion/KeywordAndSnippetTests.cs`
- [X] T095e [US4] Run US4-focused tests and verify all normal/odd scenarios pass before phase sign-off, then run post-test coverage gate (`pwsh scripts/quality/Verify-CoreCoverage.ps1`)

---

## Phase 8: User Story 6 - Basic SQL Formatting (Priority: P2)

**Goal**: Format Document and Format Selection commands with uppercase keywords, proper indentation, JOINs on new lines

**Independent Test**: Validate normal formatting output and odd cases (partial selection, syntax errors, nested edge cases) with deterministic expectations.

### Models for User Story 6

- [ ] T096 [P] [US6] Create `KeywordCase` enum in `src/SqlEssentials.Core/Models/KeywordCase.cs`
- [ ] T097 [P] [US6] Create `IndentStyle` enum in `src/SqlEssentials.Core/Models/IndentStyle.cs`
- [ ] T098 [P] [US6] Create `CommaPosition` enum in `src/SqlEssentials.Core/Models/CommaPosition.cs`
- [ ] T099 [P] [US6] Create `FormattingProfile` model in `src/SqlEssentials.Core/Models/FormattingProfile.cs`
- [ ] T100 [P] [US6] Create `FormattingResult` model in `src/SqlEssentials.Core/Models/FormattingResult.cs`
- [ ] T101 [P] [US6] Create `FormattingError` model in `src/SqlEssentials.Core/Models/FormattingError.cs`

### Formatter Implementation

- [ ] T102 [US6] Create `ISqlFormatter` interface in `src/SqlEssentials.Core/Formatting/ISqlFormatter.cs` (from contracts/)
- [ ] T103 [US6] Create `IFormattingProfile` interface in `src/SqlEssentials.Core/Formatting/IFormattingProfile.cs`
- [ ] T104 [US6] Create `IFormattingResult` interface in `src/SqlEssentials.Core/Formatting/IFormattingResult.cs`
- [ ] T105 [US6] Create `SqlFormatter` implementation in `src/SqlEssentials.Core/Formatting/SqlFormatter.cs`
- [ ] T106 [US6] Create `FormattingVisitor` using ScriptDOM in `src/SqlEssentials.Core/Formatting/FormattingVisitor.cs`
- [ ] T107 [US6] Implement keyword uppercasing in `FormattingVisitor` (FR-022)
- [ ] T108 [US6] Implement indentation logic in `FormattingVisitor` (FR-022)
- [ ] T109 [US6] Implement JOINs on new lines with ON indented in `FormattingVisitor` (FR-022)
- [ ] T110 [US6] Implement subquery indentation relative to parent in `FormattingVisitor`

### Format Selection Support

- [ ] T111 [US6] Implement `FormatSelection` method in `SqlFormatter` (FR-021)
- [ ] T112 [US6] Create default formatting profile JSON in `src/SqlEssentials.Core/Formatting/Resources/DefaultProfile.json`

### Extension Commands for Formatting

- [ ] T113 [US6] Create `FormatDocumentCommand` in `src/SqlEssentials.Extension/Commands/FormatDocumentCommand.cs` (FR-020)
- [ ] T114 [US6] Create `FormatSelectionCommand` in `src/SqlEssentials.Extension/Commands/FormatSelectionCommand.cs` (FR-021)
- [ ] T115 [US6] Register commands with menu and Ctrl+K, Ctrl+D shortcut (NFR-008)

**Checkpoint**: User Story 6 complete - Format Document and Format Selection work correctly

### Logging Instrumentation for User Story 6

- [ ] T115a [US6] Add Info-level logging to `FormatDocumentCommand` and `FormatSelectionCommand` — log command invoked, selection range, profile used
- [ ] T115b [US6] Add Debug-level logging to `SqlFormatter` — log token count processed, formatting elapsed time

### Test Coverage for User Story 6 (Normal + Odd Cases)

- [ ] T115c [P] [US6] Add formatter happy-path tests (document/selection formatting, join indentation, uppercase keywords) in `tests/SqlEssentials.Core.Tests/Formatting/SqlFormatterTests.cs`
- [ ] T115d [P] [US6] Add odd-case tests (malformed SQL fallback, deeply nested subqueries, mixed whitespace styles) in `tests/SqlEssentials.Core.Tests/Formatting/SqlFormatterEdgeCaseTests.cs`
- [ ] T115e [US6] Run US6-focused tests and verify all normal/odd scenarios pass before phase sign-off, then run post-test coverage gate (`pwsh scripts/quality/Verify-CoreCoverage.ps1`)

---

## Phase 9: User Story 7 - Custom Snippet Management (Priority: P3)

**Goal**: Settings UI for creating, editing, and managing custom snippets

**Independent Test**: Validate normal create/edit/delete flows and odd persistence/import/export cases (invalid JSON, duplicate shortcuts, missing fields).

**Depends on**: US4 (SnippetManager must exist)

### Snippet Persistence

- [ ] T116 [US7] Implement custom snippet storage in `%APPDATA%\SqlEssentials\snippets.json` (NFR-005)
- [ ] T117 [US7] Implement `SaveAsync()` and `ReloadAsync()` in `SnippetManager` (FR-018)
- [ ] T118 [US7] Implement `AddCustomSnippet`, `UpdateCustomSnippet`, `RemoveCustomSnippet` in `SnippetManager`
- [ ] T119 [US7] Implement `ExportToJson` and `ImportFromJson` in `SnippetManager`

### Options Page for Snippets

- [ ] T120 [US7] Create `SnippetOptionsPage` in `src/SqlEssentials.Extension/Options/SnippetOptionsPage.cs` (FR-018)
- [ ] T121 [US7] Create `SnippetOptionsControl` WPF control in `src/SqlEssentials.Extension/Options/SnippetOptionsControl.xaml`
- [ ] T122 [US7] Implement snippet list view with Add/Edit/Delete buttons in `SnippetOptionsControl`
- [ ] T123 [US7] Create `SnippetEditorDialog` for editing snippet details in `src/SqlEssentials.Extension/Options/SnippetEditorDialog.xaml`
- [ ] T124 [US7] Implement placeholder preview in `SnippetEditorDialog` (FR-019)

**Checkpoint**: User Story 7 complete - custom snippets can be created and managed via settings

### Logging Instrumentation for User Story 7

- [ ] T124a [US7] Add Info-level logging to snippet persistence — log save/reload/import/export operations with snippet count

### Test Coverage for User Story 7 (Normal + Odd Cases)

- [ ] T124b [P] [US7] Add snippet persistence happy-path tests (save/reload/create/update/delete/import/export) in `tests/SqlEssentials.Core.Tests/Snippets/SnippetManagerTests.cs`
- [ ] T124c [P] [US7] Add odd-case tests (invalid snippet JSON, duplicate shortcuts, placeholder mismatch validation) in `tests/SqlEssentials.Core.Tests/Snippets/SnippetManagerEdgeCaseTests.cs`
- [ ] T124d [US7] Run US7-focused tests and verify all normal/odd scenarios pass before phase sign-off, then run post-test coverage gate (`pwsh scripts/quality/Verify-CoreCoverage.ps1`)

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Telemetry, logging, settings, error handling, and final validation

### Settings Infrastructure

- [ ] T125 [P] Create `ISettingsManager` in `src/SqlEssentials.Core/Settings/ISettingsManager.cs`
- [ ] T126 [P] Create `SettingsManager` in `src/SqlEssentials.Core/Settings/SettingsManager.cs` (NFR-005, NFR-006, NFR-007)
- [ ] T127 Implement JSON settings persistence at `%APPDATA%\SqlEssentials\settings.json`

### Telemetry & Logging

- [ ] T128 [P] Create `ITelemetryService` in `src/SqlEssentials.Core/Telemetry/ITelemetryService.cs` (NFR-001)
- [ ] T129 [P] Create `TelemetryService` with anonymous metrics in `src/SqlEssentials.Core/Telemetry/TelemetryService.cs`
- [ ] T130 Implement opt-out setting for telemetry (NFR-003)
- [ ] T131 **[DONE via T054e]** ~~Create `ILogger` interface in `src/SqlEssentials.Core/Logging/ILogger.cs`~~ — delivered in Phase 2 structured logging
- [ ] T132 **[DONE via T054h]** ~~Create `FileLogger` writing to `%APPDATA%\SqlEssentials\logs\`~~ — delivered in Phase 2 structured logging (writes to `%TEMP%`)

### Keyboard Shortcuts

- [ ] T133 Create keyboard shortcut configuration in settings (NFR-009)
- [ ] T134 Implement shortcut conflict detection with warning (NFR-010)

### Error Handling

- [ ] T135 Implement global exception handler in `SqlEssentialsPackage` (SC-007)
- [ ] T136 Add graceful degradation when connection lost (serve cached suggestions)
- [ ] T137 Implement debouncing for rapid typing (30-80ms) with request cancellation

### Final Validation

- [ ] T138 Run all `quickstart.md` validation scenarios
- [ ] T139 **[PERF]** Final autocomplete latency validation: ≤100ms (target 50ms) with 500+ table database under all clause contexts (SC-001)
- [ ] T140 Verify <50MB memory usage for 1000 table cache (SC-005)
- [ ] T141 **[PERF]** Profile and optimize any completion path exceeding 80ms (leave headroom for edge cases)
- [ ] T142 Update README.md with installation and usage instructions

### Test Coverage & Validation Gates (Normal + Odd Cases)

- [ ] T142a Add/maintain test suites for each implemented phase feature covering both normal and odd cases in `tests/SqlEssentials.Core.Tests/`
- [ ] T142b Execute phase-focused test runs and require all normal/odd test cases to pass before marking any phase checkpoint complete
- [ ] T142c Enforce post-test coverage gate (`pwsh scripts/quality/Verify-CoreCoverage.ps1`) after each phase-focused test run
- [ ] T142d Enforce regression run (`dotnet test` full core suite) after each phase completion
- [ ] T142e Update phase notes/checkpoints with explicit evidence that normal + odd-case validation and coverage gate validation passed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Stories (Phase 3-9)**: All depend only on Foundational phase completion
  - All P1/P2 stories can now proceed **in parallel** after Foundational
  - US7 (P3) requires US4 to be complete (SnippetManager dependency)
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies (After Restructure)

| Story | Priority | Dependencies | Can Parallelize With |
|-------|----------|--------------|---------------------|
| US1 (Column Completion) | P1 | Foundational only | US2, US3, US4, US6 |
| US2 (Clause-Aware) | P1 | Foundational only | US1, US3, US4, US6 |
| US3 (JOIN Predicates) | P2 | Foundational only | US1, US2, US4, US6 |
| US4 (Snippets) | P2 | Foundational only | US1, US2, US3, US6 |
| US6 (Formatting) | P2 | Foundational only | US1, US2, US3, US4 |
| US7 (Custom Snippets) | P3 | US4 (SnippetManager) | — |

### True Independence Achieved ✅

After Foundational phase, each story:
- Has its own models in separate files
- Extends shared infrastructure without modifying core interfaces
- Can be tested independently
- Can be deployed independently

### Within Each Phase

- Models marked [P] can run in parallel
- Interfaces before implementations
- Core implementations before Extension integrations
- Each phase must include tests for normal and odd scenarios, and phase sign-off requires those tests to pass
- After phase tests pass, run coverage verification (`pwsh scripts/quality/Verify-CoreCoverage.ps1`) as a mandatory post-test check

### Parallel Opportunities

**After Foundational (Phase 2) completes:**
```
┌─────────────────────────────────────────────────────────┐
│                    FOUNDATIONAL                         │
└────────────────────────┬────────────────────────────────┘
                         │
    ┌────────┬───────┬───┴───┬────────┬────────┐
    ▼        ▼       ▼       ▼        ▼        │
  [US1]    [US2]   [US3]   [US4]    [US6]      │
  Column   Clause  JOIN    Snippet  Format     │
    │        │       │       │        │        │
    └────────┴───────┴───────┴────────┴────────┘
                         │
                         ▼
                      [US7] (requires US4)
                   Custom Snippets
```

---

## Implementation Strategy

### MVP First (Foundational + US1 + US2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (Schema Caching + Completion Shell)
3. Complete Phase 3: User Story 1 (Column Completion)
4. Complete Phase 4: User Story 2 (Clause-Aware)
5. **STOP and VALIDATE**: Test independently with SSMS
6. Deploy/demo as MVP

### Parallel Team Strategy

With multiple developers after Foundational:
- Developer A: US1 (Column Completion)
- Developer B: US3 (JOIN) or US4 (Snippets)
- Developer C: US6 (Formatting)
- All work in parallel, integrate at end

### Incremental Delivery

Each story adds value without breaking previous stories:
1. MVP complete → Test and validate
2. Add any P2 story → Test independently → Deploy
3. Each story is a shippable increment

---

## Notes

- **[P]** tasks = different files, no dependencies on incomplete tasks
- **[Story]** label maps task to specific user story for traceability
- Tasks without [Story] label are Setup, Foundational, or Polish tasks
- Each user story is now **truly independent** after Foundational
- Only exception: US7 depends on US4 (must have SnippetManager to extend it)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
