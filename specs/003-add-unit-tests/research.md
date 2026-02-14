# Phase 0 Research: Unit Test Expansion Strategy

## Decision 1: Scope this phase to `SqlEssentials.Core` only

- **Decision**: Limit this phase to unit tests for `SqlEssentials.Core`; defer `SqlEssentials.Extension` tests.
- **Rationale**: Highest regression risk resides in core deterministic logic; this keeps runtime/cost bounded and aligns with clarified scope.
- **Alternatives considered**:
  - Include extension tests now: rejected due to larger surface area and slower delivery for pre-phase-5 hardening.

## Decision 2: Use Table-Driven Tests (TDT) for completion behavior

- **Decision**: Use table-driven tests as the primary pattern for clause prioritization and completion ranking validation.
- **Rationale**: Completion behavior is rule-heavy and deterministic; TDT maximizes scenario density with minimal duplication.
- **Alternatives considered**:
  - One test method per clause: rejected due to repetitive setup and lower maintainability.

## Decision 3: Implement only two in-scope testability splits

- **Decision**: Implement `Suggestion Scoring Policy` and `Clause Detection Adapter` splits only.
- **Rationale**: These two splits unlock most high-value deterministic test coverage with minimal refactor risk.
- **Alternatives considered**:
  - Implement all proposed splits: rejected as out-of-scope for this phase and likely to delay test hardening.

## Decision 4: Mock metadata provider boundary

- **Decision**: Mock/fake metadata loading at the `SmoMetadataLoader` boundary to keep tests DB-independent.
- **Rationale**: Prevents flaky infra dependencies and keeps suite runtime under the 90-second budget.
- **Alternatives considered**:
  - Live DB-backed unit tests: rejected due to nondeterminism and slower execution.

## Decision 5: Use virtual clock for expiration logic

- **Decision**: Introduce clock abstraction for cache age checks and advance time in tests explicitly.
- **Rationale**: Removes `Task.Delay`/real-time waiting and enables deterministic expiration coverage.
- **Alternatives considered**:
  - Sleep-based expiration assertions: rejected due to flakiness and performance cost.

## Decision 6: Enforce per-module dual coverage gates

- **Decision**: Require both line and branch coverage >=85% for each high-risk module (`Completion`, `Context`, `Metadata`, `Logging`).
- **Rationale**: Per-module thresholds prevent aggregate coverage from masking under-tested critical modules.
- **Alternatives considered**:
  - Overall project-only gate: rejected because it can pass while specific high-risk modules fail quality.

## Decision 7: Sequencing strategy

- **Decision**: Insert this test-hardening phase immediately before roadmap phase 5.
- **Rationale**: Stabilizes already delivered behavior before introducing additional feature complexity.
- **Alternatives considered**:
  - Postpone testing until after phase 5: rejected due to increased regression blast radius.
