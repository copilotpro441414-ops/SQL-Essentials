# Tasks: Unit Test Expansion Strategy

**Input**: Design documents from `/specs/003-add-unit-tests/`  
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, `[US3]`)
- Include exact file paths in descriptions

## Phase 1: Setup (Project Initialization)

**Purpose**: Prepare test project structure and quality-gate scaffolding for core-only test phase

- [X] T001 Create test folder structure for core modules in `tests/SqlEssentials.Core.Tests/Completion/`, `tests/SqlEssentials.Core.Tests/Context/`, `tests/SqlEssentials.Core.Tests/Metadata/`, `tests/SqlEssentials.Core.Tests/Logging/`, `tests/SqlEssentials.Core.Tests/Shared/`
- [X] T002 Configure coverage collector package and test settings in `tests/SqlEssentials.Core.Tests/SqlEssentials.Core.Tests.csproj`
- [X] T003 [P] Create coverage runsettings file for line/branch collection in `tests/SqlEssentials.Core.Tests/coverage.runsettings`
- [X] T004 [P] Create shared assertion/helpers base for deterministic tests in `tests/SqlEssentials.Core.Tests/Shared/TestAssertions.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared seams and fixtures required by all user stories

**⚠️ CRITICAL**: No user story work should begin until this phase is complete

- [X] T005 Create deterministic metadata fixture builder in `tests/SqlEssentials.Core.Tests/Shared/TestMetadataBuilder.cs`
- [X] T006 Create fake metadata loader for DB-independent tests in `tests/SqlEssentials.Core.Tests/Shared/FakeMetadataLoader.cs`
- [X] T007 Create virtual clock test double in `tests/SqlEssentials.Core.Tests/Shared/FakeClock.cs`
- [X] T008 Introduce clock abstraction interface in `src/SqlEssentials.Core/Metadata/IClock.cs`
- [X] T009 [P] Implement production system clock in `src/SqlEssentials.Core/Metadata/SystemClock.cs`
- [X] T010 Refactor `SchemaCache` to consume `IClock` instead of direct UTC calls in `src/SqlEssentials.Core/Metadata/SchemaCache.cs`
- [X] T011 Create coverage gate verification script for core modules in `scripts/quality/Verify-CoreCoverage.ps1`

**Checkpoint**: Foundation ready for independent user story implementation

---

## Phase 3: User Story 1 - Protect Existing Core Behavior (Priority: P1) 🎯 MVP

**Goal**: Protect completion/context behavior with deterministic tests and required two testability splits

**Independent Test**: Run only US1 tests and verify clause prioritization, alias-qualified completion, scoring, and context parsing behavior

### Tests for User Story 1

- [X] T012 [P] [US1] Add table-driven clause prioritization tests in `tests/SqlEssentials.Core.Tests/Completion/CompletionClausePriorityTests.cs`
- [X] T013 [P] [US1] Add alias-qualified completion tests in `tests/SqlEssentials.Core.Tests/Completion/CompletionAliasQualifiedTests.cs`
- [X] T014 [P] [US1] Add scoring policy unit tests (prefix, clause bonus, tie order) in `tests/SqlEssentials.Core.Tests/Completion/SuggestionScoringPolicyTests.cs`
- [X] T015 [P] [US1] Add clause classification tests for SELECT/FROM/WHERE/JOIN/ON/GROUP BY/ORDER BY/HAVING in `tests/SqlEssentials.Core.Tests/Context/ClauseClassifierTests.cs`
- [X] T016 [P] [US1] Add context token/non-code edge-case tests in `tests/SqlEssentials.Core.Tests/Context/ContextAnalyzerEdgeCaseTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Extract scoring policy into isolated component in `src/SqlEssentials.Core/Completion/SuggestionScoringPolicy.cs`
- [X] T018 [US1] Refactor completion engine to use scoring policy component in `src/SqlEssentials.Core/Completion/CompletionEngine.cs`
- [X] T019 [US1] Extract clause classification logic into adapter/component in `src/SqlEssentials.Core/Context/ClauseClassifier.cs`
- [X] T020 [US1] Refactor context analyzer to use clause classifier adapter in `src/SqlEssentials.Core/Context/ContextAnalyzer.cs`

**Checkpoint**: US1 is independently testable and protects core completion/context behavior

---

## Phase 4: User Story 2 - Validate Cache and Logging Reliability (Priority: P1)

**Goal**: Ensure cache lifecycle and logging primitives are regression-safe and deterministic

**Independent Test**: Run only US2 tests and verify cache transitions, expiration/refresh behavior, and logging semantics

### Tests for User Story 2

- [X] T021 [P] [US2] Add cache miss/hit/invalidate lifecycle tests in `tests/SqlEssentials.Core.Tests/Metadata/SchemaCacheLifecycleTests.cs`
- [X] T022 [P] [US2] Add virtual-clock expiration and stale-while-refresh tests in `tests/SqlEssentials.Core.Tests/Metadata/SchemaCacheExpirationTests.cs`
- [X] T023 [P] [US2] Add cache refresh failure fallback tests in `tests/SqlEssentials.Core.Tests/Metadata/SchemaCacheRefreshFailureTests.cs`
- [X] T024 [P] [US2] Add null logger no-op behavior tests in `tests/SqlEssentials.Core.Tests/Logging/NullLoggerTests.cs`
- [X] T025 [P] [US2] Add log scope elapsed-time lifecycle tests in `tests/SqlEssentials.Core.Tests/Logging/LogScopeTests.cs`
- [X] T026 [P] [US2] Add log-level filtering behavior tests in `tests/SqlEssentials.Core.Tests/Logging/LoggerLevelFilteringTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Update `SchemaCache` constructors/usages for injected `IClock` and loader seam in `src/SqlEssentials.Core/Metadata/SchemaCache.cs`
- [X] T028 [US2] Adjust cache-related models/helpers to support deterministic test assertions in `src/SqlEssentials.Core/Metadata/DatabaseCache.cs`
- [X] T029 [US2] Adjust logging scope behavior if needed to satisfy deterministic tests in `src/SqlEssentials.Core/Logging/LogScope.cs`

**Checkpoint**: US2 is independently testable and validates infrastructure reliability

---

## Phase 5: User Story 3 - Adopt Safe Delivery Strategy (Priority: P2)

**Goal**: Reflect branch/phase strategy in roadmap so test phase is explicitly before existing phase 5

**Independent Test**: Review roadmap docs and verify dedicated testing phase exists before existing phase 5 work

### Tests for User Story 3

- [X] T030 [P] [US3] Add roadmap consistency check notes in `specs/003-add-unit-tests/quickstart.md`

### Implementation for User Story 3

- [X] T031 [US3] Insert dedicated pre-phase-5 testing phase tasks in `specs/1-smart-autocomplete/tasks.md`
- [X] T032 [US3] Update phase numbering/headers after insertion in `specs/1-smart-autocomplete/tasks.md`
- [X] T033 [US3] Add branch strategy note (feature branch then merge-back) in `specs/1-smart-autocomplete/tasks.md`

**Checkpoint**: US3 is independently verifiable via updated planning documents

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final quality gates and documentation alignment across stories

- [X] T034 [P] Run full core unit test suite with coverage settings in `tests/SqlEssentials.Core.Tests/coverage.runsettings`
- [X] T035 Enforce per-module coverage gate in `scripts/quality/Verify-CoreCoverage.ps1` (Completion/Context >=85%, Metadata >=75/70%, Logging >=75/70%)
- [X] T036 [P] Update test execution instructions and expected outputs in `specs/003-add-unit-tests/quickstart.md`
- [X] T037 Validate `contracts/testing-phase.openapi.yaml` examples align with final quality-gate behavior in `specs/003-add-unit-tests/contracts/testing-phase.openapi.yaml`

---

## Phase 7: Testing Refinements (Post-Completion Quality Improvements)

**Purpose**: Strengthen quality gates with differentiated thresholds, mutation testing, and runtime enforcement

- [X] T038 [P] Update per-module coverage thresholds (Completion/Context=85%, Metadata=75/70%, Logging=75/70%) in `scripts/quality/Verify-CoreCoverage.ps1`
- [X] T039 [P] Create Stryker mutation testing configuration targeting SuggestionScoringPolicy in `stryker-config.json`
- [X] T040 [P] Create mutation spot-check script with .NET Framework fallback in `scripts/quality/Run-MutationSpotCheck.ps1`
- [X] T041 [P] Create test runtime budget enforcement script (90s gate) in `scripts/quality/Verify-TestRuntime.ps1`
- [X] T042 Update quickstart with mutation testing and runtime budget documentation in `specs/003-add-unit-tests/quickstart.md`
- [X] T043 Run full verification sequence (coverage + mutation + runtime) and confirm all gates pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies
- **Phase 2 (Foundational)**: depends on Phase 1; blocks all user stories
- **Phase 3 (US1)**: depends on Phase 2; delivers MVP
- **Phase 4 (US2)**: depends on Phase 2; can run in parallel with US1 if staffed, but recommended after US1 for MVP focus
- **Phase 5 (US3)**: depends on Phase 2; documentation/roadmap update
- **Phase 6 (Polish)**: depends on completion of selected user stories
- **Phase 7 (Refinements)**: depends on Phase 6; strengthens quality gates

### User Story Dependencies

- **US1 (P1)**: independent after foundational tasks
- **US2 (P1)**: independent after foundational tasks
- **US3 (P2)**: independent after foundational tasks

### Within Each User Story

- Write tests first and confirm failure state
- Implement/refactor minimal code to satisfy tests
- Validate story independently before moving on

---

## Parallel Execution Examples

### User Story 1

- Run in parallel: `T012`, `T013`, `T014`, `T015`, `T016` (different test files)
- Run in sequence: `T017` -> `T018`; `T019` -> `T020`

### User Story 2

- Run in parallel: `T021`, `T022`, `T023`, `T024`, `T025`, `T026`
- Run in sequence: `T027` -> `T028` -> `T029`

### User Story 3

- Run in sequence: `T031` -> `T032` -> `T033`
- `T030` can run in parallel with `T031`

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Setup (Phase 1)
2. Complete Foundational (Phase 2)
3. Complete US1 (Phase 3)
4. Validate US1 independently (completion/context deterministic tests)

### Incremental Delivery

1. Deliver US1 (core behavior protection)
2. Deliver US2 (cache/logging reliability)
3. Deliver US3 (roadmap/phase strategy alignment)
4. Run polish quality gates

### Parallel Team Strategy

- Developer A: US1 tests + scoring/clause split implementation
- Developer B: US2 cache/logging tests
- Developer C: US3 roadmap updates and final polish docs

---

## Notes

- All tasks follow strict checklist format with Task ID, optional `[P]`, optional `[US#]`, and file path.
- This feature intentionally excludes `SqlEssentials.Extension` tests per clarified scope.
- Deferred split candidates remain out-of-scope for this tasks file.
