---

description: "Task list for structured diagnostic logging"
---

# Tasks: Structured Diagnostic Logging

**Input**: Design documents from `/specs/002-structured-logging/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not requested in the feature specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project scaffolding for the logging feature

- [X] T001 Add shared logging constants in src/SqlEssentials.Core/Logging/LoggingConstants.cs
- [X] T002 Add log path builder utility in src/SqlEssentials.Extension/Logging/LogPathBuilder.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [X] T003 [P] Add LogLevel enum in src/SqlEssentials.Core/Logging/LogLevel.cs
- [X] T004 [P] Add LogEntry and LogExceptionDetails models in src/SqlEssentials.Core/Logging/LogEntry.cs
- [X] T005 [P] Add LogFileOptions in src/SqlEssentials.Core/Logging/LogFileOptions.cs
- [X] T006 [P] Add ILogger interface in src/SqlEssentials.Core/Logging/ILogger.cs
- [X] T007 [P] Add ILogScope interface in src/SqlEssentials.Core/Logging/ILogScope.cs
- [X] T008 Add LogScope implementation in src/SqlEssentials.Core/Logging/LogScope.cs
- [X] T009 Add NullLogger implementation in src/SqlEssentials.Core/Logging/NullLogger.cs
- [X] T010 Add JSON Lines serializer in src/SqlEssentials.Core/Logging/LogEntrySerializer.cs
- [X] T011 Add bounded log queue and background worker in src/SqlEssentials.Extension/Logging/LogWriteQueue.cs
- [X] T012 Add file sink with rotation in src/SqlEssentials.Extension/Logging/FileLogger.cs
- [X] T013 Wire logger shutdown flush/close in src/SqlEssentials.Extension/SqlEssentialsPackage.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 2 - Control Logging via Environment Variable (Priority: P1)

**Goal**: Allow enabling/disabling and tuning verbosity without VS settings changes.

**Independent Test**: Set SQL_ESSENTIALS_LOG_LEVEL=Debug, launch VS, perform SQL editing. Verify Debug-level entries appear. Unset variable, restart VS, verify no log file is written.

### Implementation for User Story 2

- [X] T014 [US2] Parse SQL_ESSENTIALS_LOG_LEVEL and map to LogLevel in src/SqlEssentials.Extension/Logging/LoggerFactory.cs
- [X] T015 [US2] Parse SQL_ESSENTIALS_LOG_MAX_MB and apply defaults in src/SqlEssentials.Extension/Logging/LoggerFactory.cs
- [X] T016 [US2] Enforce Off/unset fast path to avoid file I/O in src/SqlEssentials.Extension/Logging/LoggerFactory.cs and src/SqlEssentials.Extension/Logging/FileLogger.cs

**Checkpoint**: Logging can be enabled/disabled via environment variables

---

## Phase 4: User Story 1 - Diagnose Extension Behavior via Log File (Priority: P1) 🎯 MVP

**Goal**: Provide a complete chronological trace of extension activity in a single log file.

**Independent Test**: Enable logging, open a SQL file, type a query, close the file. Open the log file and verify it contains timestamped entries covering package initialization, content type detection, completion trigger, context analysis, cache lookup, suggestion generation, and elapsed times.

### Implementation for User Story 1

- [X] T017 [US1] Log package initialization and version info in src/SqlEssentials.Extension/SqlEssentialsPackage.cs
- [X] T018 [P] [US1] Instrument completion trigger/response in src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs
- [X] T019 [P] [US1] Instrument completion engine start/end and suggestion counts in src/SqlEssentials.Core/Completion/CompletionEngine.cs
- [X] T020 [P] [US1] Instrument context analysis start/end in src/SqlEssentials.Core/Context/ContextAnalyzer.cs
- [X] T021 [P] [US1] Instrument schema cache load/refresh/expiry in src/SqlEssentials.Core/Metadata/SchemaCache.cs
- [X] T022 [P] [US1] Instrument SMO metadata load and errors in src/SqlEssentials.Core/Metadata/SmoMetadataLoader.cs
- [X] T023 [US1] Instrument content type detection command in src/SqlEssentials.Extension/Commands/LogContentTypeCommand.cs
- [X] T024 [US1] Add Error-level logging with exception details in src/SqlEssentials.Core/Completion/CompletionEngine.cs and src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs

**Checkpoint**: Diagnostic log includes all required component events

---

## Phase 5: User Story 3 - Trace Performance Across Components (Priority: P2)

**Goal**: Provide per-stage timing breakdowns with correlation IDs.

**Independent Test**: Enable Trace-level logging, trigger a completion, open the log file. Verify each pipeline stage has its own timed entry and all entries for the same request share a correlation ID.

### Implementation for User Story 3

- [X] T025 [US3] Generate correlation ID per completion request in src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs
- [X] T026 [US3] Propagate correlation ID through completion pipeline in src/SqlEssentials.Core/Completion/CompletionEngine.cs
- [X] T027 [US3] Add timed LogScope around context analysis in src/SqlEssentials.Core/Context/ContextAnalyzer.cs
- [X] T028 [US3] Add timed LogScope around schema cache access in src/SqlEssentials.Core/Metadata/SchemaCache.cs
- [X] T029 [US3] Add timed LogScope around completion engine work in src/SqlEssentials.Core/Completion/CompletionEngine.cs
- [X] T030 [US3] Log cache refresh duration and object counts in src/SqlEssentials.Core/Metadata/SmoMetadataLoader.cs

**Checkpoint**: Each pipeline stage has timed, correlated entries

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cleanup and consistency across the codebase

- [X] T031 [P] Replace Debug.WriteLine in src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs with structured logging
- [X] T032 [P] Replace Debug.WriteLine and perf.log writes in src/SqlEssentials.Core/Completion/CompletionEngine.cs with structured logging
- [X] T033 [P] Replace Debug.WriteLine and perf.log writes in src/SqlEssentials.Extension/Commands/LogContentTypeCommand.cs with structured logging
- [X] T034 [P] Replace perf.log writes in src/SqlEssentials.Extension/Completion/SqlCompletionSource.cs with structured logging
- [X] T035 Validate quickstart steps in specs/002-structured-logging/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 2 (Phase 3)**: Depends on Foundational completion
- **User Story 1 (Phase 4)**: Depends on User Story 2 (logging must be enabled)
- **User Story 3 (Phase 5)**: Depends on User Story 1 (baseline instrumentation)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 2 (P1)**: Must be done before US1 to enable logging
- **User Story 1 (P1)**: Requires US2; enables diagnostic coverage
- **User Story 3 (P2)**: Requires US1 for component instrumentation

---

## Parallel Example: User Story 2

```bash
Task: "Parse SQL_ESSENTIALS_LOG_LEVEL and map to LogLevel in src/SqlEssentials.Extension/Logging/LoggerFactory.cs"
Task: "Parse SQL_ESSENTIALS_LOG_MAX_MB and apply defaults in src/SqlEssentials.Extension/Logging/LoggerFactory.cs"
```

---

## Parallel Example: User Story 1

```bash
Task: "Instrument completion engine start/end and suggestion counts in src/SqlEssentials.Core/Completion/CompletionEngine.cs"
Task: "Instrument context analysis start/end in src/SqlEssentials.Core/Context/ContextAnalyzer.cs"
Task: "Instrument schema cache load/refresh/expiry in src/SqlEssentials.Core/Metadata/SchemaCache.cs"
Task: "Instrument SMO metadata load and errors in src/SqlEssentials.Core/Metadata/SmoMetadataLoader.cs"
```

---

## Parallel Example: User Story 3

```bash
Task: "Add timed LogScope around context analysis in src/SqlEssentials.Core/Context/ContextAnalyzer.cs"
Task: "Add timed LogScope around schema cache access in src/SqlEssentials.Core/Metadata/SchemaCache.cs"
Task: "Add timed LogScope around completion engine work in src/SqlEssentials.Core/Completion/CompletionEngine.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 2
4. Complete Phase 4: User Story 1
5. **STOP and VALIDATE**: Run the independent test for User Story 1

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 2 → Test independently → Deploy/Demo
3. Add User Story 1 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Polish and cross-cutting cleanup

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 2
   - Developer B: User Story 1 (start once US2 done)
   - Developer C: User Story 3 (start once US1 done)
