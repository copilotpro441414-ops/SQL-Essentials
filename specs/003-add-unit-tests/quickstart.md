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

## Success Metrics Verification (85% High-Risk Coverage)

- Define high-risk modules: Completion, Context, Metadata Cache, Logging.
- Collect line + branch coverage per module.
- Gate rules:
    - Each high-risk module coverage >= 85% line and >= 85% branch.
  - Full unit test runtime <= 90s.
  - Mandatory regression tests pass for clause prioritization, alias qualification, cache transitions, and null logger behavior.
- CI gate fails if any module drops below threshold or runtime exceeds budget.
