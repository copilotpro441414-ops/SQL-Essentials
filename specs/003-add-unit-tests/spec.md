# Feature Specification: Unit Test Expansion Strategy

**Feature Branch**: `003-add-unit-tests`  
**Created**: 2026-02-15  
**Status**: Draft  
**Input**: User description: "I need to add some unit tests to the app in order to make sure that all various places of the application are working as expected. I need you to analyze all the details about the project and come up with a detailed suggestion for how we can make the unit tests for this project. The unit tests should be serving a purpose for making sure that different places of the application are working as expected. If you think that a few places on the application need to be split into smaller testable areas, also let me know what are your suggestions here. Also based on the current status of the tasks .Md file. Tell me your suggestion about whether we should create a separate branch and proceed with adding unit tests and then merge them back to the current branch and continue implementing the tasks specified. Or we should update the tasks by another phase which is before the upcoming phase that was not yet implemented which is phase 5 and push phase 5 forward and add another upcoming phase that is about adding unit tests to the application. Tell me which way is better or your suggestion here"

## Clarifications

### Session 2026-02-15

- Q: Which coverage gate model should be enforced for success criteria? → A: Require both line and branch coverage >= 85% for each high-risk module (Completion, Context, Metadata, Logging).
- Q: Which testability splits are in-scope for this phase? → A: Implement only Scoring Policy and Clause Detection splits now; keep other splits as deferred candidates.
- Q: What is the test scope boundary for this phase? → A: Scope this phase to `SqlEssentials.Core` unit tests only; defer `SqlEssentials.Extension` tests.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Protect Existing Core Behavior (Priority: P1)

As a maintainer, I need reliable unit tests for currently implemented autocomplete and context logic so changes do not break working behavior in phases 1-4.

**Why this priority**: The current implementation already contains delivered functionality; regressions here would invalidate progress before phase 5 starts.

**Independent Test**: Can be fully tested by running only core behavior tests for context detection, alias resolution, ranking, and suggestion generation, and validating deterministic outputs for representative SQL inputs.

**Acceptance Scenarios**:

1. **Given** a valid query and cursor position in a FROM clause, **When** completion is requested, **Then** table/view suggestions are prioritized over columns.
2. **Given** a valid query with `alias.` input and matching alias binding, **When** completion is requested, **Then** only columns for the resolved table are returned.
3. **Given** a typed token prefix, **When** completion is requested, **Then** prefix matches rank higher than non-prefix matches.

---

### User Story 2 - Validate Cache and Logging Reliability (Priority: P1)

As a maintainer, I need unit tests for cache freshness/refresh behavior and logging primitives so reliability risks are caught before new feature work.

**Why this priority**: Cache and logging are cross-cutting infrastructure used by current and upcoming phases; defects here propagate broadly.

**Independent Test**: Can be fully tested by executing tests for cache hit/miss/expiration/refresh/error-state transitions and tests for logger level behavior and scope lifecycle behavior.

**Acceptance Scenarios**:

1. **Given** an empty cache key, **When** metadata is requested, **Then** a fresh load is triggered and cache state transitions to ready on success.
2. **Given** an expired cache entry, **When** metadata is requested, **Then** stale data is served and refresh starts asynchronously.
3. **Given** logging is disabled via null logger behavior, **When** logging APIs are invoked, **Then** no exceptions occur and execution continues.

---

### User Story 3 - Adopt a Safe Delivery Strategy (Priority: P2)

As a project owner, I need a clear plan for introducing tests without blocking the roadmap, including branch strategy and task-phase sequencing.

**Why this priority**: Delivery planning affects throughput and merge risk but depends on technical test scope defined in higher-priority stories.

**Independent Test**: Can be tested by confirming a documented branch + phase strategy exists, is reflected in task tracking, and is actionable by the team.

**Acceptance Scenarios**:

1. **Given** the current task state where phase 5 is not started, **When** planning is finalized, **Then** a dedicated unit-testing phase is inserted before phase 5 and subsequent phases are shifted.
2. **Given** normal feature development flow, **When** the team begins test implementation, **Then** work occurs on a dedicated branch and is merged back after green test results.

### Edge Cases

- How should tests behave when SQL input is empty, whitespace-only, or cursor position is out of range?
- How should tests validate behavior when parser errors exist but partial context is still expected?
- How should caching behavior be validated when refresh fails but an older cache entry exists?
- How should completion behavior be validated when alias binding exists but target table cannot be resolved?
- How should ranking tests handle ties and ordering stability across suggestion types?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project MUST add unit tests for all currently implemented core completion behaviors delivered through phases 1-4.
- **FR-002**: The test suite MUST verify clause-aware prioritization behavior for SELECT, FROM, WHERE, JOIN, ON, GROUP BY, ORDER BY, and HAVING contexts.
- **FR-003**: The test suite MUST verify alias-qualified completion behavior, including resolved alias, unresolved alias, and direct-table qualifier paths.
- **FR-004**: The test suite MUST verify typed-token extraction and suggestion ranking behavior, including prefix-boost ordering.
- **FR-005**: The test suite MUST verify context analysis behavior for alias extraction, partial input detection, qualifier detection, and non-code context detection.
- **FR-006**: The test suite MUST verify metadata cache behavior for miss, hit, expiration, refresh, invalidation, and error recovery flows.
- **FR-007**: The test suite MUST verify logging primitives for level filtering, null logger no-op behavior, and scope elapsed-time lifecycle.
- **FR-008**: The test suite MUST include deterministic test fixtures that represent common SQL authoring patterns and edge cases.
- **FR-009**: The test plan MUST identify high-risk untested areas and map each area to specific test cases and expected outcomes.
- **FR-010**: The team MUST introduce a dedicated unit-test implementation phase before currently planned phase 5 work.
- **FR-011**: Unit-test implementation MUST be delivered on a dedicated branch and merged into the active feature branch only after tests are consistently passing.
- **FR-012**: Task tracking MUST be updated to include explicit test tasks, ownership, and completion criteria aligned to each covered component.
- **FR-013**: The test strategy MUST define a minimum per-component baseline covering happy path, invalid input, and failure-mode behavior.
- **FR-013a**: The test strategy MUST enforce per-module coverage gates for each high-risk module (`Completion`, `Context`, `Metadata`, `Logging`) with both line coverage and branch coverage >= 85%.
- **FR-014**: Where behavior cannot be isolated cleanly, the plan MUST identify refactoring candidates that split logic into smaller testable units without changing user-visible behavior.
- **FR-015**: This phase MUST implement exactly two testability splits: `Suggestion Scoring Policy` and `Clause Detection Adapter`.
- **FR-016**: Remaining split candidates (`Token and Qualifier Parsing`, `Cache Freshness Policy`, `Status Transition Dispatcher`) MUST be documented as deferred and MUST NOT block this phase.
- **FR-017**: This phase MUST target unit tests in `SqlEssentials.Core` only; `SqlEssentials.Extension` test coverage is explicitly deferred.

### Key Entities *(include if feature involves data)*

- **Test Coverage Area**: A functional module to be validated (completion engine, context analysis, metadata cache, logging), with defined risk and target scenarios.
- **Test Case Specification**: A single test definition containing setup input, trigger action, and verifiable expected result.
- **Refactoring Candidate**: A specific logic segment recommended for decomposition to improve isolated unit testing and maintainability.
- **Phase Plan Update**: A revision to task sequencing that inserts a dedicated unit-test phase before phase 5.
- **Branch Strategy Decision**: The delivery policy stating that test work is isolated in a dedicated branch and merged after validation.

## Suggested Coverage Map

### Completion Behavior Coverage

- Verify clause-priority behavior separately for object suggestions and column suggestions.
- Verify scoring outcomes for prefix match, clause bonus, and key-column relevance ordering.
- Verify qualifier resolution path precedence: alias full name, alias table name, direct qualifier lookup.

### Context Analysis Coverage

- Validate clause detection for common query forms and partially typed statements.
- Validate alias extraction for single-table, multi-join, and duplicate-alias conflict scenarios.
- Validate token parsing for bracketed identifiers, dotted prefixes, and boundary cursor positions.
- Validate non-code context gating for comments and string literals.

### Metadata Cache Coverage

- Validate state transitions for initial load, ready-state hit, expired-state refresh, and refresh failure.
- Validate invalidation behavior for one connection key and full-cache invalidation.
- Validate status-change signaling consistency for each transition.

### Logging Coverage

- Validate null logger no-op behavior across all logging entry points.
- Validate scope lifecycle with begin/end behavior and elapsed-time capture.
- Validate level-based acceptance/rejection behavior for representative log levels.

## Proposed Testability Splits

### In Scope (Current Phase)

- **Suggestion Scoring Policy Split**: Extract scoring/ordering rules into an isolated policy unit to test ranking without full completion setup.
- **Clause Detection Adapter Split**: Separate parser traversal from clause classification so classification rules can be tested independently from parser wiring.

### Deferred Candidates (Future Phase)

- **Token and Qualifier Parsing Split**: Isolate partial-token and qualifier parsing into dedicated parsing helpers for focused boundary testing.
- **Cache Freshness Policy Split**: Isolate expiration and refresh-decision logic into a pure policy unit to test time-based behavior deterministically.
- **Status Transition Dispatcher Split**: Separate cache state transitions from event publication to verify state and notification behavior independently.

## Assumptions

- Current functionality from phases 1-4 is considered the baseline and should be protected before adding phase 5 features.
- Existing test project remains the primary location for new core unit tests.
- `SqlEssentials.Extension` unit testing is outside this phase scope and tracked as deferred work.
- Unit tests focus on deterministic behavior and do not depend on live database connectivity.
- Performance-focused tasks remain separate from unit tests and are not blocked by this feature unless behavioral failures are found.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each high-risk module (`Completion`, `Context`, `Metadata`, `Logging`) achieves >= 85% line coverage and >= 85% branch coverage.
- **SC-002**: 100% of identified regression-critical behaviors (clause prioritization, alias-qualified completion, cache lifecycle states, null logger behavior) have at least one passing unit test each.
- **SC-003**: The full unit-test suite completes in under 90 seconds on a standard developer machine.
- **SC-004**: For two consecutive merge attempts, no regression defects are reported in the previously delivered phase 1-4 behaviors after integrating test changes.
- **SC-005**: Task tracking explicitly shows a dedicated pre-phase-5 testing phase with all included test tasks marked independently verifiable.
