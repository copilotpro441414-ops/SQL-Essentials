# Implementation Plan: Unit Test Expansion Strategy

**Branch**: `003-add-unit-tests` | **Date**: 2026-02-15 | **Spec**: `specs/003-add-unit-tests/spec.md`
**Input**: Feature specification from `/specs/003-add-unit-tests/spec.md`

## Summary

Add a dedicated pre-Phase-5 testing phase for `SqlEssentials.Core` only, focusing on deterministic regression protection for delivered Phase 1-4 behaviors. This phase introduces two required testability splits (Scoring Policy and Clause Detection Adapter), adds table-driven tests for completion behavior, validates metadata cache and logging infrastructure, and enforces per-module coverage gates of >=85% line and >=85% branch for `Completion`, `Context`, `Metadata`, and `Logging`.

## Technical Context

**Language/Version**: C# on .NET Framework 4.8  
**Primary Dependencies**: xUnit 2.6.*, Moq 4.20.*, Microsoft.NET.Test.Sdk 17.*, ScriptDOM, SMO wrappers  
**Storage**: In-memory fixtures and test doubles (no external storage)  
**Testing**: xUnit unit tests + table-driven tests + mock-based infrastructure tests + coverage gates  
**Target Platform**: Windows developer machines and CI for .NET Framework test execution  
**Project Type**: Core library + extension host; this phase targets core library tests only  
**Performance Goals**: Full unit-test suite completes in <90 seconds  
**Constraints**: No live database dependency; deterministic tests only; extension tests deferred; no user-visible behavior changes from refactors  
**Scale/Scope**: `SqlEssentials.Core` modules `Completion`, `Context`, `Metadata`, `Logging`; implement exactly two testability splits in this phase

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Async-First / Non-Blocking UI**: PASS — async paths (cache load/refresh) remain validated without introducing blocking UI behavior.
- **II. Core Engine Separation**: PASS — scope explicitly targets independently testable core engine logic.
- **III. ScriptDOM + SMO Foundation**: PASS — tests validate behavior around existing ScriptDOM parsing and SMO-backed metadata boundaries.
- **IV. Performance-First**: PASS — explicit runtime gate (<90s tests) and coverage of latency-critical ranking logic.
- **V. Profile-Driven Configuration**: N/A — no profile/config feature change.
- **VI. Privacy-First AI**: N/A — no AI feature scope.
- **VII. Previewable & Undoable Transformations**: N/A — no refactoring/formatting UX transformations in scope.

**Gate Decision (Pre-Research)**: PASS

## Project Structure

### Documentation (this feature)

```text
specs/003-add-unit-tests/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── testing-phase.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── SqlEssentials.Core/
│   ├── Completion/
│   ├── Context/
│   ├── Metadata/
│   └── Logging/
└── SqlEssentials.Extension/

tests/
└── SqlEssentials.Core.Tests/
   ├── Completion/
   ├── Context/
   ├── Metadata/
   └── Logging/
```

**Structure Decision**: Keep all implementation in `SqlEssentials.Core` and `tests/SqlEssentials.Core.Tests`. `SqlEssentials.Extension` testing is explicitly deferred and not part of this phase.

## Phase Breakdown (Pre-Phase-5 Testing Phase)

1. **Task 1: Implement Scoring Policy Split**  
  Extract and unit test scoring/ordering rules as an isolated pure policy.

2. **Task 2: Implement Clause Detection Adapter Split**  
  Separate parser traversal from clause classification and add targeted classifier tests.

3. **Task 3: Build Table-Driven Completion Tests**  
  Add TDT scenarios for SELECT/FROM/WHERE/JOIN/ON/GROUP BY/ORDER BY/HAVING and alias-qualified completions.

4. **Task 4: Add Metadata Cache Infrastructure Tests**  
  Cover miss/hit/expire/refresh/error/invalidation using a virtual clock and mocked metadata loader.

5. **Task 5: Add Logging Infrastructure Tests**  
  Cover level filtering semantics, null logger no-op behavior, and scope elapsed-time behavior.

6. **Task 6: Enforce Coverage and Runtime Gates**  
  Require per-module >=85% line + >=85% branch coverage for high-risk modules and total test runtime <90 seconds.

7. **Task 7: Update Roadmap Sequencing**  
  Insert this testing phase before existing phase 5 in the main roadmap/tasks flow.

## Complexity Tracking

No constitution violations requiring justification.

## Post-Design Constitution Re-Check

- **I. Async-First / Non-Blocking UI**: PASS
- **II. Core Engine Separation**: PASS
- **III. ScriptDOM + SMO Foundation**: PASS
- **IV. Performance-First**: PASS

**Gate Decision (Post-Design)**: PASS
