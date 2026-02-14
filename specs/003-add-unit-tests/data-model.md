# Data Model: Unit Test Expansion Strategy

## 1) TestCoverageArea

- **Description**: Logical module under test with risk and coverage targets.
- **Fields**:
  - `id` (string, required) — unique identifier, e.g., `completion`, `context`, `metadata-cache`, `logging`
  - `name` (string, required)
  - `riskLevel` (enum, required): `High | Medium | Low`
  - `targetCoveragePercent` (int, required, range 0-100)
  - `runtimeBudgetMs` (int, optional)
  - `owner` (string, optional)
- **Validation Rules**:
  - `targetCoveragePercent >= 85` for `riskLevel=High`
  - `id` must be stable and unique for CI reporting

## 2) TestCaseSpec

- **Description**: Single test case definition for deterministic behavior validation.
- **Fields**:
  - `id` (string, required)
  - `areaId` (string, required, FK -> TestCoverageArea.id)
  - `scenario` (string, required)
  - `input` (object, required)
  - `expected` (object, required)
  - `type` (enum, required): `TableDriven | Unit | Infrastructure`
  - `isEdgeCase` (bool, required)
- **Validation Rules**:
  - `input` and `expected` must be serializable and deterministic
  - each high-risk area must contain at least one happy path, one invalid-input case, and one failure-mode case

## 3) ClausePriorityRow (TDT)

- **Description**: Table-driven row for SQL completion clause prioritization.
- **Fields**:
  - `name` (string, required)
  - `sql` (string, required)
  - `cursorPosition` (int, required)
  - `connectionKey` (string, required)
  - `expectedTopType` (enum, required): `Table | View | Column | Function | Keyword`
  - `mustContain` (string[], optional)
  - `mustNotContain` (string[], optional)
- **Validation Rules**:
  - `cursorPosition` must be within `[0..sql.Length]`
  - `mustContain` and `mustNotContain` cannot overlap

## 4) MetadataFixture

- **Description**: Mocked schema snapshot used by completion/cache tests.
- **Fields**:
  - `connectionKey` (string, required)
  - `tables` (array, required)
  - `views` (array, optional)
  - `relationships` (array, optional)
  - `lastRefreshedUtc` (datetime, required)
- **Validation Rules**:
  - object names should match expected SQL parser casing rules for deterministic comparisons

## 5) CacheTimeline

- **Description**: Virtual-time timeline for cache expiration and refresh tests.
- **Fields**:
  - `startUtc` (datetime, required)
  - `ttlMinutes` (int, required)
  - `advanceEvents` (array<int>, required) — minutes to advance per step
  - `expectedStateSequence` (enum[], required): `Empty | Loading | Ready | Refreshing`
- **Validation Rules**:
  - `expectedStateSequence` length must equal `advanceEvents.Length + 1`

## 6) CoverageGate

- **Description**: Quality gate definition for CI and local verification.
- **Fields**:
  - `scope` (enum, required): `Module | Feature`
  - `moduleId` (string, optional)
  - `lineCoverageMin` (int, required)
  - `branchCoverageMin` (int, required)
  - `maxRuntimeSeconds` (int, required)
- **Validation Rules**:
  - `lineCoverageMin` and `branchCoverageMin` must be >= 85 for high-risk modules
  - `maxRuntimeSeconds <= 90` for full unit-test suite gate

## Relationships

- `TestCoverageArea 1 -> many TestCaseSpec`
- `TestCoverageArea 1 -> many CoverageGate`
- `MetadataFixture` is referenced by `ClausePriorityRow` and cache lifecycle tests
- `CacheTimeline` is referenced by cache expiration/refesh test cases

## State Transitions

### CacheLifecycle

- `Empty -> Loading -> Ready`
- `Ready -> Refreshing -> Ready` (successful refresh)
- `Ready -> Refreshing -> Ready` (refresh failure with stale cache retained)
- `Any -> Empty` (invalidation)

### TestExecution

- `Defined -> Implemented -> Passing -> Gated`
- `Passing -> Failing` (regression detected)
