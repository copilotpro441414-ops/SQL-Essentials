# Quickstart: Pre-Phase-5 Unit Testing Phase

## Objective

Add a dedicated test-hardening phase before roadmap Phase 5 to protect Phase 1-4 behavior with deterministic, fast (<90s), DB-independent unit tests for `SqlEssentials.Core` only.

## Scope Boundary

- In scope: `SqlEssentials.Core` unit tests (`Completion`, `Context`, `Metadata`, `Logging`).
- Out of scope (deferred): `SqlEssentials.Extension` unit tests.
- In-scope testability splits: `Suggestion Scoring Policy`, `Clause Detection Adapter`.
- Deferred splits: `Token and Qualifier Parsing`, `Cache Freshness Policy`, `Status Transition Dispatcher`.

## Phase Breakdown (Execution Order)

1. **Refactor Scoring for Testability (In Scope)**
   - Extract scoring/ranking into a pure policy unit.
   - Add focused ranking tests for prefix boost, clause bonus, and PK ordering.

2. **Refactor Clause Detection for Testability (In Scope)**
   - Split clause classification logic from ScriptDOM traversal wiring.
   - Add classifier-only unit tests for SELECT/FROM/WHERE/JOIN/ON/GROUP BY/ORDER BY/HAVING.

3. **Implement Table-Driven Tests (Core Logic)**
   - Add TDT suites for completion prioritization and alias-qualified completion.
   - Cover happy path + invalid input + edge cases.

4. **Implement Infrastructure Tests**
   - Metadata cache: miss/hit/expire/refresh/error/invalidate.
   - Logging: null logger no-op, level filtering behavior, scope lifecycle.

5. **Add Quality Gates**
   - Enforce high-risk logic coverage >= 85%.
   - Enforce full test runtime <= 90 seconds.

6. **Insert Test Phase Into Main Task Roadmap**
   - Add this phase before current Phase 5 and shift subsequent phases.

## Table-Driven Test (TDT) Template for Clause Prioritization

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using Xunit;

public sealed class CompletionClausePriorityTests
{
    public static IEnumerable<object[]> ClauseCases()
    {
        yield return new object[]
        {
            "FROM prioritizes table/view",
            "SELECT * FROM ",
            14,
            ClauseType.From,
            SuggestionType.Table,
            new[] { "Users", "Orders" },
            Array.Empty<string>()
        };

        yield return new object[]
        {
            "SELECT prioritizes columns",
            "SELECT u. FROM dbo.Users u",
            9,
            ClauseType.Select,
            SuggestionType.Column,
            new[] { "UserId", "UserName" },
            new[] { "Orders" }
        };

        yield return new object[]
        {
            "WHERE prioritizes columns",
            "SELECT * FROM dbo.Users u WHERE ",
            32,
            ClauseType.Where,
            SuggestionType.Column,
            new[] { "UserId" },
            Array.Empty<string>()
        };
    }

    [Theory]
    [MemberData(nameof(ClauseCases))]
    public async Task Returns_expected_priority_for_clause(
        string caseName,
        string sql,
        int cursor,
        ClauseType expectedClause,
        SuggestionType expectedTopType,
        string[] mustContain,
        string[] mustNotContain)
    {
        var metadata = TestMetadataBuilder.Default();
        var cache = new FakeSchemaCache(metadata);
        var analyzer = new ContextAnalyzer(NullLogger.Instance);
        var engine = new CompletionEngine(analyzer, cache, NullLogger.Instance);

        var result = await engine.GetCompletionsAsync(
            sql,
            cursor,
            "conn-A",
            TriggerReason.Typing,
            correlationId: caseName);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Suggestions);
        Assert.Equal(expectedTopType, result.Suggestions.First().Type);

        foreach (var expected in mustContain)
        {
            Assert.Contains(result.Suggestions, s => s.DisplayText.Equals(expected, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var forbidden in mustNotContain)
        {
            Assert.DoesNotContain(result.Suggestions, s => s.DisplayText.Equals(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }
}
```

## Mocking Strategy (DB-Independent)

- Mock or fake the metadata provider/loader at the boundary (`SmoMetadataLoader` dependency).
- Use stable in-memory `DatabaseCache` fixtures for all completion and cache tests.
- Use deterministic fixture builders (`TestMetadataBuilder`) with explicit table/view/column topology.
- Avoid any network, file system, or SQL Server access in unit tests.
- Keep parser-dependent tests minimal and deterministic by using fixed SQL strings.

## Virtual Clock Strategy (No Delays)

- Introduce a clock abstraction:
  - `public interface IClock { DateTimeOffset UtcNow { get; } }`
- Production implementation uses system time; tests use `FakeClock` with manual advance:
  - `fakeClock.Advance(TimeSpan.FromMinutes(31));`
- Cache expiration policy reads `clock.UtcNow` instead of direct `DateTimeOffset.UtcNow`.
- Test flow:
  1. Seed cache at `t0`.
  2. Advance virtual clock by less than TTL and assert cache hit.
  3. Advance beyond TTL and assert stale-while-refresh behavior.

## Success Metrics Verification (Differentiated Coverage Gates)

- Define high-risk modules: Completion, Context, Metadata Cache, Logging.
- Collect line + branch coverage per module.
- Gate rules (differentiated by module risk profile):
  - **Completion**: line >= 85%, branch >= 85% (pure logic, high user impact).
  - **Context**: line >= 85%, branch >= 85% (pure logic, high user impact).
  - **Metadata**: line >= 75%, branch >= 70% (infrastructure boundary, some paths integration-only).
  - **Logging**: line >= 75%, branch >= 70% (thin wrappers, diminishing returns past 75%).
  - Full unit test runtime <= 90s.
  - Mandatory regression tests pass for clause prioritization, alias qualification, cache transitions, and null logger behavior.
- CI gate fails if any module drops below its threshold or runtime exceeds budget.

### Running Coverage Gates

```powershell
# Auto-discover latest coverage report
pwsh .\scripts\quality\Verify-CoreCoverage.ps1

# Use a specific report file
pwsh .\scripts\quality\Verify-CoreCoverage.ps1 -CoverageReportPath .\TestResults\abc\coverage.cobertura.xml

# Advisory mode (no exit-code failure)
pwsh .\scripts\quality\Verify-CoreCoverage.ps1 -FailOnViolation $false
```

## Mutation Testing (Scoring Policy Spot-Check)

### Why Mutation Testing?

Code coverage measures execution, not fault detection. A test suite can achieve 100% line coverage
with weak assertions (`Assert.True(true)`) and still miss real bugs. Mutation testing introduces
small logic changes (mutations) into the code and checks that at least one test catches each
mutation. A high mutation score proves your tests have meaningful assertions.

### Scope

Only `SuggestionScoringPolicy.cs` is targeted — this is the single most user-visible ranking
component. If its tests don't catch mutations in scoring logic, coverage is a false signal.

### Running the Spot-Check

```powershell
# Run with Stryker.NET (if installed)
pwsh .\scripts\quality\Run-MutationSpotCheck.ps1

# Install Stryker.NET (one-time)
dotnet tool install -g dotnet-stryker
```

### Manual Fallback (if Stryker is unavailable for .NET Framework 4.8)

If Stryker cannot run against the .NET Framework 4.8 project, verify test quality manually:

1. **Mutation 1: Swap prefix bonus sign**
   - In `SuggestionScoringPolicy.cs`, change `score += 50` (the prefix bonus) to `score -= 50`.
   - Run tests — expect at least one failure in `SuggestionScoringPolicyTests.cs`.
   - Revert the change.

2. **Mutation 2: Swap a comparison operator**
   - Change a `>` to `<` in a bonus condition check.
   - Run tests — expect at least one failure.
   - Revert the change.

3. **Mutation 3: Remove a clause bonus**
   - Delete an entire clause bonus case arm (e.g., the FROM clause bonus).
   - Run tests — expect at least one failure.
   - Revert the change.

If all 3 mutations cause test failures, scoring tests have adequate fault detection.

## Test Runtime Budget

### Why a Runtime Budget?

Test suites silently degrade over time as new tests are added. A hard gate prevents the "boiling
frog" effect where cumulative slowdown makes the suite impractical for developer workflow.

### Current Budget

- **90 seconds** for the full `SqlEssentials.Core.Tests` suite on a standard developer machine.

### Running the Budget Check

```powershell
# Standard check (90s budget)
pwsh .\scripts\quality\Verify-TestRuntime.ps1

# Custom budget
pwsh .\scripts\quality\Verify-TestRuntime.ps1 -MaxSeconds 120

# Skip rebuild (faster if already built)
pwsh .\scripts\quality\Verify-TestRuntime.ps1 -NoBuild
```

### What to Do if the Budget is Exceeded

1. **Profile slow tests**: `dotnet test --logger "console;verbosity=detailed"`
2. **Move expensive setup to shared fixtures** — look for tests creating heavy objects repeatedly.
3. **Replace real I/O with fakes** — look for any `Task.Delay` or file system access.
4. **Split categories** — if integration tests are added later, separate fast/slow runs.

## Full Quality Gate Verification Sequence

Run all gates together after implementation changes:

```powershell
# Step 1: Build and run tests with coverage
dotnet test .\tests\SqlEssentials.Core.Tests\SqlEssentials.Core.Tests.csproj `
  --settings .\tests\SqlEssentials.Core.Tests\coverage.runsettings `
  --collect:"XPlat Code Coverage"

# Step 2: Verify per-module coverage gates
pwsh .\scripts\quality\Verify-CoreCoverage.ps1

# Step 3: Mutation spot-check on scoring policy
pwsh .\scripts\quality\Run-MutationSpotCheck.ps1

# Step 4: Verify runtime budget
pwsh .\scripts\quality\Verify-TestRuntime.ps1 -NoBuild

# All four passing = quality gates satisfied
```
