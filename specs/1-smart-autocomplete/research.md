# Research: SQL Essentials MVP — Technology Decisions & Best Practices

**Phase**: 0 (Outline & Research)  
**Date**: 2026-02-04  
**Status**: Complete

## Overview

This document captures technology decisions, best practices research, and rationale for the SQL Essentials MVP implementation.

---

## 1. VSIX Extension Architecture

### Decision: Use VSPackage with Async Package Loading

**Rationale**: SSMS is built on the Visual Studio Shell. Modern VSIX extensions should use `AsyncPackage` to avoid blocking the IDE startup.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Legacy `Package` base class | Blocks VS startup; deprecated pattern |
| MEF-only extension | Insufficient for menu commands and options pages |

**Implementation Notes**:
- Derive from `AsyncPackage` (not `Package`)
- Use `ProvideAutoLoad` with `PackageAutoLoadFlags.BackgroundLoad`
- Initialize services in `InitializeAsync` method
- Use `JoinableTaskFactory` for all UI-thread marshaling

**References**:
- [AsyncPackage documentation](https://learn.microsoft.com/en-us/visualstudio/extensibility/how-to-use-asyncpackage-to-load-vspackages-in-the-background)
- [Community.VisualStudio.Toolkit](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit)

---

## 2. Completion Provider Architecture

### Decision: Implement `IAsyncCompletionSource` (Modern Completion API)

**Rationale**: The modern async completion API provides better performance, cancellation support, and filtering capabilities compared to the legacy `ICompletionSource`.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Legacy `ICompletionSource` | Synchronous; poor performance for large suggestion sets |
| Custom popup window | Doesn't integrate with native SSMS/VS completion UI |

**Implementation Notes**:
- Implement `IAsyncCompletionSource` for suggestion generation
- Implement `IAsyncCompletionItemManager` for custom filtering/sorting
- Use `CompletionContext.TriggerReason` to distinguish explicit vs implicit triggers
- Support cancellation via `CancellationToken` in all async methods

**Key Interfaces**:
```csharp
// Core completion source
public interface IAsyncCompletionSource
{
    Task<CompletionContext> GetCompletionContextAsync(
        IAsyncCompletionSession session,
        CompletionTrigger trigger,
        SnapshotPoint triggerLocation,
        SnapshotSpan applicableToSpan,
        CancellationToken token);
}
```

---

## 3. ScriptDOM Usage Patterns

### Decision: Parse on Every Keystroke with Debouncing

**Rationale**: ScriptDOM parsing is fast enough (<10ms for typical queries) to run on each keystroke after debounce. This provides accurate context without maintaining complex incremental parse state.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Incremental parsing | ScriptDOM doesn't support incremental; would require custom parser |
| Parse only on explicit trigger | Misses context for implicit triggers (after dot, after keyword) |

**Implementation Notes**:
- Use `TSqlParser` for the target SQL Server version (default: TSql160)
- Handle parse errors gracefully — incomplete queries are common during typing
- Extract visitor pattern for AST traversal (`TSqlFragmentVisitor`)
- Cache last successful parse for fallback when current text has errors

**Key Classes**:
```csharp
// Parser selection
var parser = new TSql160Parser(initialQuotedIdentifiers: false);

// Parse with error handling
IList<ParseError> errors;
TSqlFragment fragment = parser.Parse(new StringReader(sql), out errors);

// AST traversal
public class ClauseVisitor : TSqlFragmentVisitor
{
    public override void Visit(SelectStatement node) { /* ... */ }
    public override void Visit(FromClause node) { /* ... */ }
}
```

---

## 4. SMO Metadata Access

### Decision: Async Wrappers over SMO with Connection Pooling

**Rationale**: SMO operations are synchronous and can be slow. Wrapping in `Task.Run` with proper connection management prevents UI blocking while leveraging SMO's comprehensive metadata access.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Direct SQL queries to sys views | More code; loses SMO's version abstraction |
| Third-party metadata library | Adds dependency; SMO is already available in SSMS |

**Implementation Notes**:
- Use `ServerConnection` with connection pooling (reuse connections)
- Wrap blocking SMO calls in `Task.Run` with cancellation support
- Prefetch common metadata (tables, columns, FKs) on connection
- Use `SetDefaultInitFields` to optimize SMO property loading

**Performance Optimizations**:
```csharp
// Optimize SMO property loading
server.SetDefaultInitFields(typeof(Table), "Name", "Schema", "IsSystemObject");
server.SetDefaultInitFields(typeof(Column), "Name", "DataType", "InPrimaryKey");

// Async wrapper pattern
public async Task<IReadOnlyList<TableInfo>> GetTablesAsync(CancellationToken ct)
{
    return await Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        return database.Tables.Cast<Table>()
            .Where(t => !t.IsSystemObject)
            .Select(t => new TableInfo(t))
            .ToList();
    }, ct);
}
```

---

## 5. Schema Cache Strategy

### Decision: Per-Database In-Memory Cache with TTL and Manual Refresh

**Rationale**: Caching schema metadata in memory provides instant autocomplete while TTL-based refresh handles schema changes. Per-database scoping prevents stale cross-database references.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| No cache (always query) | Too slow; violates 100ms target |
| Persistent disk cache | Adds complexity; stale data risk; minimal benefit for MVP |
| Global cache across databases | Risk of cross-database pollution |

**Implementation Notes**:
- Use `ConcurrentDictionary<string, DatabaseCache>` keyed by `server:database`
- Each `DatabaseCache` contains: tables, views, columns, FKs, procedures, functions
- Default TTL: 30 minutes (configurable)
- Background refresh on TTL expiry while serving stale data
- Manual refresh command clears and reloads cache

**Cache Structure**:
```csharp
public class SchemaCache
{
    private readonly ConcurrentDictionary<string, DatabaseCache> _caches = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);

    public async Task<DatabaseCache> GetOrLoadAsync(string connectionKey, CancellationToken ct)
    {
        if (_caches.TryGetValue(connectionKey, out var cache) && !cache.IsExpired(_ttl))
            return cache;

        // Load in background, return stale if available
        _ = RefreshCacheAsync(connectionKey, ct);
        return cache ?? await WaitForCacheAsync(connectionKey, ct);
    }
}
```

---

## 6. Testing Strategy

### Decision: xUnit with Test Fixtures and Golden File Tests

**Rationale**: xUnit provides modern testing features, parallel execution, and good VS integration. Golden file tests are ideal for formatter output validation.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| MSTest | Less flexible; fewer community extensions |
| NUnit | Similar to xUnit; team preference for xUnit |

**Implementation Notes**:
- **Unit tests**: `SqlEssentials.Core.Tests` — test all core logic without VS dependencies
- **Integration tests**: `SqlEssentials.Integration.Tests` — use VS SDK test host for extension tests
- **Golden file tests**: Store expected formatter output in `.expected.sql` files
- **Performance tests**: Benchmark completion latency with large schema fixtures

**Test Categories**:
```csharp
// Unit test example
public class ContextAnalyzerTests
{
    [Theory]
    [InlineData("SELECT | FROM Users", ClauseType.Select)]
    [InlineData("SELECT * FROM |", ClauseType.From)]
    [InlineData("SELECT * FROM Users WHERE |", ClauseType.Where)]
    public void DetectsClauseAtCursor(string queryWithCursor, ClauseType expected)
    {
        var (query, position) = ParseCursor(queryWithCursor);
        var analyzer = new ContextAnalyzer();
        var result = analyzer.GetClauseAtPosition(query, position);
        Assert.Equal(expected, result);
    }
}

// Golden file test example
public class FormatterTests
{
    [Theory]
    [MemberData(nameof(GetGoldenFileTestCases))]
    public void FormatsMatchGoldenFile(string inputPath, string expectedPath)
    {
        var input = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);
        var formatter = new SqlFormatter();
        var actual = formatter.Format(input);
        Assert.Equal(expected, actual);
    }
}
```

---

## 7. Snippet System Design

### Decision: JSON-Based Snippet Definitions with TextMate-Style Placeholders

**Rationale**: JSON is human-readable and easily editable. TextMate-style placeholders (`$1`, `${2:default}`) are a widely understood standard used in VS Code and other editors.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| XML snippets (VS format) | Verbose; harder to edit manually |
| Custom format | Learning curve; no tooling support |

**Snippet Format**:
```json
{
  "snippets": [
    {
      "shortcut": "selj",
      "name": "SELECT with JOIN",
      "body": [
        "SELECT ${1:columns}",
        "FROM ${2:table1} ${3:t1}",
        "JOIN ${4:table2} ${5:t2} ON ${3:t1}.${6:key} = ${5:t2}.${6:key}",
        "WHERE ${7:condition}"
      ],
      "placeholders": {
        "1": { "default": "*" },
        "2": { "default": "TableName" },
        "6": { "default": "Id" }
      }
    }
  ]
}
```

**Implementation Notes**:
- Built-in snippets shipped in extension resources
- Custom snippets stored in `%APPDATA%\SqlEssentials\snippets.json`
- Snippet expansion via `ITextEdit` with tracking spans for placeholders
- Tab navigation between placeholders using `ITextView` caret manipulation

---

## 8. Formatting Engine Design

### Decision: AST-Based Formatting with Visitor Pattern

**Rationale**: Using ScriptDOM's AST for formatting ensures semantic correctness. Visitor pattern provides clean separation of formatting rules.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Token-based formatting | Loses semantic context; harder to handle complex cases |
| Regex-based formatting | Fragile; can't handle nested structures |

**Implementation Notes**:
- Parse SQL to AST using ScriptDOM
- Walk AST with formatting visitor that emits formatted output
- Preserve comments by tracking token positions
- Apply formatting profile settings (indent style, keyword case, etc.)

**Formatting Profile Schema**:
```json
{
  "name": "Default",
  "keywordCase": "UPPER",
  "indentStyle": "spaces",
  "indentSize": 4,
  "commasPosition": "trailing",
  "joinOnNewLine": true,
  "onPredicateIndent": true,
  "selectColumnsOnSeparateLines": false,
  "maxLineLength": 120
}
```

---

## 9. Error Handling & Logging

### Decision: Structured Logging with ActivityTraceSource + User-Visible Error Toasts

**Rationale**: ActivityTraceSource integrates with VS diagnostic tools. Toast notifications provide non-intrusive error feedback to users.

**Implementation Notes**:
- Use `ActivityLog` for diagnostic logging visible in VS ActivityLog.xml
- Use `VS.StatusBar` for transient status messages
- Use `VS.MessageBox` only for critical errors requiring user action
- Never throw exceptions that crash SSMS; catch and log at boundaries

**Error Handling Pattern**:
```csharp
public async Task<IEnumerable<Suggestion>> GetSuggestionsAsync(CancellationToken ct)
{
    try
    {
        return await _engine.ComputeSuggestionsAsync(ct);
    }
    catch (OperationCanceledException)
    {
        // Expected on cancellation; return empty
        return Enumerable.Empty<Suggestion>();
    }
    catch (Exception ex)
    {
        await ActivityLog.LogErrorAsync(nameof(CompletionSource), ex.ToString());
        return Enumerable.Empty<Suggestion>();
    }
}
```

---

## 10. SSMS Version Compatibility

### Decision: Target SSMS 18+ (VS 2017 Shell), Primary Support SSMS 21/22

**Rationale**: SSMS 18+ uses VS 2017 Shell which supports modern async patterns. Earlier versions have limited extensibility.

**Compatibility Matrix**:
| SSMS Version | VS Shell | .NET | Support Level |
|--------------|----------|------|---------------|
| SSMS 21/22 | VS 2022 | 4.8 | Primary |
| SSMS 19/20 | VS 2019 | 4.8 | Supported |
| SSMS 18 | VS 2017 | 4.7.2+ | Supported |
| SSMS 17 and earlier | VS 2015 | 4.6 | Not supported |

**Implementation Notes**:
- Target .NET Framework 4.8 for maximum compatibility
- Use `Microsoft.VisualStudio.Shell.15.0` as minimum VS SDK version
- Conditional compilation for version-specific features if needed
- Test on SSMS 21 (primary) and SSMS 18 (minimum)

---

## Summary of Key Decisions

| Area | Decision | Key Benefit |
|------|----------|-------------|
| Extension Model | AsyncPackage + MEF | Non-blocking startup |
| Completion API | IAsyncCompletionSource | Cancellation + performance |
| SQL Parsing | ScriptDOM with debounce | Accuracy + speed |
| Metadata Access | SMO with async wrappers | Comprehensive + version-safe |
| Caching | Per-database TTL cache | Instant autocomplete |
| Testing | xUnit + golden files | Coverage + formatter validation |
| Snippets | JSON + TextMate placeholders | Portable + editable |
| Formatting | AST-based visitor | Semantic correctness |
| Error Handling | Structured logging + toasts | Debuggable + user-friendly |
| Compatibility | SSMS 18+ / VS 2017 Shell | Modern patterns + wide reach |

---

**Phase 0 Complete** — Proceed to Phase 1: Design & Contracts
