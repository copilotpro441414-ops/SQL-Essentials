
## 2/4 – `sql_essentials_technical_spec.md`

```markdown
# SQL Essentials - Technical Specification

This specification is meant to be read by an AI (or human) developer implementing the **SQL Essentials** SSMS plugin. It defines:

- Architecture and main components
- Core data structures
- Key algorithms (autocomplete, JOIN suggestions, ranking)
- SSMS integration points
- Performance and compatibility constraints

---

## 1. Architecture Overview

### 1.1 High‑Level Components

SQL Essentials runs as an in‑process SSMS extension (VSIX/VSPackage) and plugs into the T‑SQL editor. [web:24][web:49]

**Layers:**

1. **SSMS Extension Layer**
   - VSPackage entry point.
   - Command registration (menus, toolbar).
   - Lifecycle management.

2. **Editor Integration Layer**
   - Access to active text view (`IWpfTextView` / `IVsTextView`). [web:18][web:45]
   - Caret and selection handling.
   - Keystroke event hooks.
   - Popup positioning.

3. **Core Services Layer**
   - T‑SQL lexer and parser.
   - Context analyzer.
   - Suggestion generator (autocomplete engine).
   - Snippet manager.
   - Code formatter (later phase).

4. **Metadata & Caching Layer**
   - Database schema repository (queries `sys.*` views).
   - Connection manager (current SSMS connection).
   - In‑memory metadata cache with TTL.

5. **Persistence & Settings Layer**
   - User preferences storage (registry/JSON).
   - Snippet storage.

```text
┌────────────────────────────────────────────┐
│ SSMS / VSIX / VSPackage (Entry)          │
└───────────────┬──────────────────────────┘
                │
        Editor Integration
                │
        Core Services (Autocomplete, Parser)
                │
        Metadata & Cache
                │
       DB & File System (external)
```


---

### 1.2 Data Flow – Autocomplete

1. User types a trigger character (`.`, space after keyword, shortcut).
2. Editor Integration captures keystroke and reads:
    - Full script text.
    - Current cursor position.
3. Context Analyzer:
    - Tokenizes text up to cursor.
    - Determines current clause (SELECT/FROM/WHERE/JOIN).
    - Extracts aliases and JOIN context.
4. Metadata Cache:
    - Provides `DatabaseSchema` for current database (possibly async).
5. Suggestion Generator:
    - Chooses suggestion type (tables, columns, functions, snippets).
    - Filters candidates by prefix and context.
    - Ranks suggestions.
6. UI:
    - Renders dropdown at caret.
    - Inserts selected suggestion back into editor buffer.

---

## 2. Core Data Structures

### 2.1 Token and Clause Types

```csharp
public enum TokenType
{
    StringLiteral,
    NumericLiteral,
    DateLiteral,
    Identifier,

    Keyword,
    BuiltInFunction,
    DataType,

    Operator,
    DotOperator,
    Punctuation,

    LineComment,
    BlockComment,

    Whitespace,
    Unknown,
    EndOfInput
}

public enum SqlClauseType
{
    Select,
    From,
    Where,
    GroupBy,
    Having,
    OrderBy,
    Join,
    On,
    Insert,
    Update,
    Delete,
    CreateTable,
    CreateView,
    CreateProcedure,
    Unknown
}
```

```csharp
public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}
```


---

### 2.2 Autocomplete Context

```csharp
public enum AutocompleteType
{
    Database,
    Schema,
    Table,
    View,
    Column,
    StoredProcedure,
    ScalarFunction,
    TableValuedFunction,
    BuiltInFunction,
    Keyword,
    Snippet,
    Alias,
    Variable,
    Parameter
}

public class TableAlias
{
    public string Alias { get; set; }          // "u"
    public string TableName { get; set; }      // "Users"
    public string SchemaName { get; set; }     // "dbo"
    public int DefiningLineNumber { get; set; }
    public List<Column> AvailableColumns { get; set; }
}
```

```csharp
public class JoinCondition
{
    public string LeftColumn { get; set; }   // "u.UserID"
    public string RightColumn { get; set; }  // "o.UserID"
    public string Operator { get; set; }     // "="
    public double Confidence { get; set; }   // 0.0 - 1.0
}

public enum JoinType
{
    Inner,
    Left,
    Right,
    Full,
    Cross,
    Unknown
}

public class JoinContext
{
    public string LeftTable { get; set; }
    public string LeftAlias { get; set; }
    public string RightTable { get; set; }
    public string RightAlias { get; set; }
    public JoinType JoinType { get; set; }
    public List<JoinCondition> ExistingConditions { get; set; }
    public List<JoinCondition> SuggestedConditions { get; set; }
}
```

```csharp
public class AutocompleteContext
{
    public int CursorPosition { get; set; }
    public string ScriptText { get; set; }

    public string CurrentDatabase { get; set; }
    public string CurrentSchema { get; set; }

    public List<Token> Tokens { get; set; }
    public Token CurrentToken { get; set; }
    public Token PreviousToken { get; set; }
    public Token TriggerToken { get; set; }

    public SqlClauseType CurrentClause { get; set; }
    public AutocompleteType SuggestType { get; set; }

    public List<TableAlias> DefinedAliases { get; set; }
    public JoinContext ActiveJoin { get; set; }

    public bool IsInsideSubquery { get; set; }
    public int ParenthesisDepth { get; set; }
}
```


---

### 2.3 Suggestions

```csharp
public enum SuggestionType
{
    Table,
    View,
    Column,
    StoredProcedure,
    Function,
    Keyword,
    Snippet,
    JoinCondition,
    JoinTemplate
}

public class Parameter
{
    public string Name { get; set; }           // "@UserId"
    public string DataType { get; set; }       // "int"
    public int? MaxLength { get; set; }
    public bool HasDefaultValue { get; set; }
    public string DefaultValue { get; set; }
}

public class SnippetVariable
{
    public string Name { get; set; }
    public string DefaultValue { get; set; }
    public string Description { get; set; }
}
```

```csharp
public class SuggestionItem
{
    public string DisplayText { get; set; }     // e.g. "dbo.Users"
    public string InsertionText { get; set; }   // text actually inserted
    public SuggestionType Type { get; set; }
    public string Description { get; set; }
    public string IconKey { get; set; }

    // DB object metadata
    public string DatabaseName { get; set; }
    public string SchemaName { get; set; }
    public string ObjectName { get; set; }

    // Columns
    public string ColumnDataType { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }

    // Routines
    public List<Parameter> Parameters { get; set; }
    public string ReturnType { get; set; }

    // Snippets
    public List<SnippetVariable> Variables { get; set; }

    // Ranking
    public double RelevanceScore { get; set; }
    public int UsageFrequency { get; set; }
}
```


---

### 2.4 Schema Metadata

```csharp
public class Column
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string DefaultValue { get; set; }
}
```

```csharp
public class TableDefinition
{
    public string TableName { get; set; }
    public string SchemaName { get; set; }

    public List<Column> Columns { get; set; }
    // Indexes, constraints etc. can be added as needed.
}

public class ViewDefinition
{
    public string ViewName { get; set; }
    public string SchemaName { get; set; }
    public List<Column> Columns { get; set; }
}

public class StoredProcedureDefinition
{
    public string ProcedureName { get; set; }
    public string SchemaName { get; set; }
    public List<Parameter> Parameters { get; set; }
}

public class UserDefinedFunctionDefinition
{
    public string FunctionName { get; set; }
    public string SchemaName { get; set; }
    public List<Parameter> Parameters { get; set; }
    public string ReturnType { get; set; }
}
```

```csharp
public class SchemaDefinition
{
    public string SchemaName { get; set; }

    public Dictionary<string, TableDefinition> Tables { get; set; }
    public Dictionary<string, ViewDefinition> Views { get; set; }
    public Dictionary<string, StoredProcedureDefinition> StoredProcedures { get; set; }
    public Dictionary<string, UserDefinedFunctionDefinition> Functions { get; set; }
}

public class DatabaseSchema
{
    public string DatabaseName { get; set; }
    public DateTime LastRefreshed { get; set; }
    public Dictionary<string, SchemaDefinition> Schemas { get; set; }

    public bool IsStale(TimeSpan ttl) =>
        DateTime.UtcNow - LastRefreshed > ttl;
}
```


---

## 3. Core Algorithms

### 3.1 T‑SQL Lexer

**Goal**: Convert script text to tokens up to the cursor. A simple state‑machine lexer is sufficient.

Key rules:

- Whitespace: `\s+` → `Whitespace`.
- Line comments: `--` to end of line → `LineComment`.
- Block comments: `/* ... */` → `BlockComment`.
- String literals: `'text'` (handle `''` escape).
- Identifiers:
    - `^[A-Za-z_][A-Za-z0-9_]*`
    - `[bracketed identifiers]`.
- Keywords: compare identifier value against T‑SQL keyword list (case‑insensitive).
- Operators: `=`, `<`, `>`, `<>`, `<=`, `>=`, `+`, `-`, `*`, `/`, `AND`, `OR`, `NOT`, etc.
- Punctuation: `(`, `)`, `,`, `;`, `.` (dot is `DotOperator`).

Interface:

```csharp
public interface ISqlLexer
{
    List<Token> Tokenize(string script);
}
```

Complexity: $O(n)$ in length of script.

---

### 3.2 Context Analysis

**Input**: script text, cursor position.
**Output**: `AutocompleteContext`.

Steps (simplified):

1. **Tokenize** only up to cursor or full script (depending on performance).
2. Find `CurrentToken` (token containing or before cursor).
3. Determine **clause**:
    - Scan backwards for clause‑head keywords (`SELECT`, `FROM`, `WHERE`, `JOIN`, `ON`, `GROUP BY`, `ORDER BY`, etc.).
    - Map to `SqlClauseType` enum.
4. Extract **aliases**:
    - Within the current statement, scan FROM/JOIN sections for patterns:
        - `schema.table alias`
        - `schema.table AS alias`
    - Populate `TableAlias` list with derived columns (from `DatabaseSchema`).
5. Detect **JOIN context**:
    - If `CurrentClause` is `Join` or `On`, attempt to find:
        - Left table/alias (previous table in FROM/JOIN chain).
        - Right table/alias (current JOIN target).
    - Create `JoinContext` with any existing ON conditions.
6. Decide **suggestion type**:
    - Dot after alias → `AutocompleteType.Column`.
    - In SELECT list (no dot) → column names and snipped suggestions.
    - In FROM/JOIN table position → tables/views.
    - In WHERE/HAVING → columns and scalar functions.
    - At beginning of line or after semicolon → keywords/snippets.

Interface:

```csharp
public interface IContextAnalyzer
{
    AutocompleteContext Analyze(int cursorPosition, string scriptText, DatabaseSchema schema);
}
```


---

### 3.3 JOIN Suggestion

Competitor behaviour: suggest key‑based and name‑based JOIN conditions. [web:16][web:44][web:46][web:50]

**Input**: `JoinContext`, `DatabaseSchema`.
**Output**: list of `JoinCondition` with confidence.

Steps:

1. Retrieve `TableDefinition` for left/right tables.
2. Detect FK relationships:
    - Use `sys.foreign_keys` and `sys.foreign_key_columns` to map parent/child. [web:44][web:46]
    - For each FK that connects left ↔ right:
        - Add condition: `LeftAlias.PK = RightAlias.FK` with high confidence (e.g. 0.95). [web:44][web:46]
3. Name‑based matches:
    - For columns where names are equal or follow common patterns (`UserID`/`ID`, `<TableName>ID` etc.), add conditions with mid confidence (e.g. 0.7). [web:44][web:46][web:50]
4. Type compatibility:
    - Only consider columns with matching or compatible types.
5. Rank:
    - Sort by confidence descending.
    - Keep top N (e.g., 5).

Example:

- `Users` with `UserID` (PK).
- `Orders` with `UserID` (FK).
- Suggestion: `u.UserID = o.UserID` with confidence ≈ 0.95. [web:44][web:46]

---

### 3.4 Column Relevance Ranking

**Goal**: rank columns for SELECT and WHERE clauses.

Factors (weights indicative):

- **Exact or prefix name match**: high weight (e.g. 100).
- **Alias usage**: columns from more recent aliases rank higher.
- **Key columns**: PK/FK flagged as more important.
- **Data type suitability**:
    - In conditions like `>`, numeric/date columns up‑ranked.
- **Common column names**: `CreatedDate`, `ModifiedDate`, `IsActive`, etc.
- **Usage frequency**: persist usage stats across sessions.

Formula (example):

```text
score = 100 * exactMatch
      + 80 * aliasRecencyFactor
      + 70 * keyFactor
      + 60 * dataTypeFactor
      + 50 * commonNameFactor
      + 40 * usageFrequencyFactor
      + 30 * notNullFactor
```

Return suggestions ordered by `RelevanceScore`.

---

## 4. SSMS Integration

### 4.1 VSIX / VSPackage

According to community guidance, SSMS extensions are created like VS extensions: [web:24][web:49]

- Create a VSIX project in Visual Studio.
- Add a `VSPackage` item (`SqlEssentialsPackage`).
- Use attributes for registration (Package GUID, menu resource).
- Build a `.vsix` that is installed or deployed into SSMS’s extension location. [web:24][web:49]

---

### 4.2 Editor Access

Typical pattern (adapted from VS extension examples): [web:18][web:45]

- Use `IVsTextView` → `IWpfTextView` to get text and caret.
- Use `ITextBuffer` for underlying text.

Pseudo‑code:

```csharp
private IWpfTextView GetWpfTextView(IVsTextView vsTextView)
{
    if (vsTextView is IVsUserData userData)
    {
        object holder;
        var guidViewHost = DefGuidList.guidIWpfTextViewHost;
        userData.GetData(ref guidViewHost, out holder);
        var viewHost = (IWpfTextViewHost)holder;
        return viewHost.TextView;
    }
    return null;
}
```

Once you have `IWpfTextView`:

- `TextSnapshot` to read text.
- `Caret.Position.BufferPosition` for cursor.
- Subscribe to `KeyDown` or text buffer change events.

---

### 4.3 Metadata Queries

To fill `DatabaseSchema`, use standard SQL Server system views: [web:23][web:50]

**Tables \& Views:**

```sql
SELECT
    s.name AS SchemaName,
    t.name AS TableName
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name;
```

```sql
SELECT
    s.name AS SchemaName,
    v.name AS ViewName
FROM sys.views v
JOIN sys.schemas s ON s.schema_id = v.schema_id;
```

**Columns:**

```sql
SELECT
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_CATALOG = DB_NAME();
```

**Primary Keys:**

```sql
SELECT
    t.name AS TableName,
    s.name AS SchemaName,
    c.name AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE i.is_primary_key = 1;
```

**Foreign Keys:**

```sql
SELECT
    fk.name AS ForeignKeyName,
    schParent.name AS ParentSchema,
    tParent.name AS ParentTable,
    schRef.name AS ReferencedSchema,
    tRef.name AS ReferencedTable,
    cParent.name AS ParentColumn,
    cRef.name AS ReferencedColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables tParent ON fkc.parent_object_id = tParent.object_id
JOIN sys.tables tRef ON fkc.referenced_object_id = tRef.object_id
JOIN sys.schemas schParent ON tParent.schema_id = schParent.schema_id
JOIN sys.schemas schRef ON tRef.schema_id = schRef.schema_id
JOIN sys.columns cParent ON fkc.parent_object_id = cParent.object_id
    AND fkc.parent_column_id = cParent.column_id
JOIN sys.columns cRef ON fkc.referenced_object_id = cRef.object_id
    AND fkc.referenced_column_id = cRef.column_id;
```

These queries are consistent with how tools like dbForge derive join suggestions. [web:44][web:46][web:50]

---

## 5. Caching \& Performance

### 5.1 Metadata Cache

```csharp
public interface IMetadataCache
{
    Task<DatabaseSchema> GetOrFetchAsync(
        string dbName,
        string connectionString,
        TimeSpan ttl,
        bool forceRefresh = false);

    void Invalidate(string dbName);
}
```

Implementation notes:

- Use `ConcurrentDictionary<string, DatabaseSchema>` for thread safety.
- TTL default: 30 minutes.
- When stale or missing, call repository → refresh.


### 5.2 Performance Targets

Aligned with user experience of similar tools: [web:42][web:46][web:50]

- Keystroke to popup: target ≤ 50 ms; max 150 ms.
- 20 suggestions generation: ≤ 100 ms.
- Initial metadata load: ≤ 500 ms on typical DB.
- Memory:
    - Per‑DB cache: target ≤ 50 MB.
    - Plugin total overhead: ≤ 200 MB in extreme cases.

---

## 6. Settings \& Configuration

### 6.1 Settings Model

```csharp
public class UserSettings
{
    public bool EnableAutocomplete { get; set; } = true;
    public int AutocompleteDelayMs { get; set; } = 150;
    public List<char> TriggerCharacters { get; set; } = new() { '.', ' ', '(', ',' };

    public bool IncludeSystemObjects { get; set; } = false;
    public int MaxSuggestions { get; set; } = 20;

    public bool EnableFormatter { get; set; } = true;
    public string FormattingProfileName { get; set; } = "Default";

    public int SchemaCacheTtlMinutes { get; set; } = 30;
}
```


### 6.2 Persistence

- Options:
    - Registry: `HKCU\Software\SQL_Essentials\Settings`.
    - Or JSON: `%APPDATA%\SqlEssentials\settings.json`.

Interface:

```csharp
public interface IUserSettingsRepository
{
    UserSettings Load();
    void Save(UserSettings settings);
}
```


---

## 7. Testing \& Quality

### 7.1 Unit Tests

- Lexer tests: edge cases for strings, comments, identifiers.
- Parser tests: clause detection, alias extraction, JOIN detection.
- Suggestion generator tests: various contexts and prefixes.
- Join suggestion tests: multiple FK scenarios, name‑only matches.
- Cache tests: TTL and invalidation behaviour.


### 7.2 Integration Tests

- Spin up a test DB schema with several related tables.
- Connect via SSMS + plugin.
- Simulate keystrokes and assert suggestions.


### 7.3 Performance Tests

- Benchmark tokenization + context analysis on large scripts.
- Load schema of 500+ tables, 5k+ columns, measure cache size and generation time.

---

## 8. Compatibility \& Constraints

- Target SSMS: 2016 and later. [web:24]
- Target SQL Server: 2012 and later.
- Must respect SSMS’s extensibility constraints:
    - No blocking UI thread.
    - Avoid undocumented internal hooks.

---

## 9. Security \& Safety

- Use parameterized queries for all DB metadata calls.
- Do not execute user SQL for analysis (only parse).
- Do not log sensitive connection details.
- Settings and snippet files should be per‑user, not system‑global.

---

## 10. Summary

This technical spec gives:

- A concrete architecture decomposition.
- Data models for context, suggestions, and schema.
- Algorithms for context analysis, JOIN suggestions, and ranking.
- SSMS integration patterns and metadata query templates.

An AI assistant (or human team) can use this as the **blueprint** to implement the SQL Essentials core engine and integration.

---
```

When you’re ready, I’ll paste:

3/4 – `sql_essentials_dev_prompt.md` (development roadmap & tasks)
<span style="display:none">[^1][^10][^11][^12][^13][^14][^15][^2][^3][^4][^5][^6][^7][^8][^9]</span>

<div align="center">⁂</div>

[^1]: https://documentation.red-gate.com/sp/sql-prompt-ai/ai-code-completion-preview
[^2]: https://www.red-gate.com/products/sql-prompt/resources/tip-22-control-code-completion-suggestions-sql-prompt/
[^3]: https://www.red-gate.com/products/sql-prompt/
[^4]: https://www.red-gate.com/hub/product-learning/sql-prompt/mastering-ai-prompts-how-to-get-the-best-out-of-sql-prompt-ai
[^5]: https://www.red-gate.com/hub/product-learning/sql-prompt/sql-intellisense-and-autocomplete-in-ssms-and-sql-prompt
[^6]: https://docs.devart.com/sqlcomplete/writing-sql-with-code-completion/inserting-suggestions-into-code.html
[^7]: https://stackoverflow.com/questions/45751908/how-to-get-iwpftextview-from-command-visual-studio-extension-2017
[^8]: http://english.cogitosoft.com/html/product/item.aspx?id=1425
[^9]: https://www.devart.com/dbforge/sql/sqlcomplete/code-completion.html
[^10]: https://www.sqlservercentral.com/forums/topic/developing-extensions-for-ssms-2016
[^11]: https://www.red-gate.com/our-company/newsroom/press-releases/redgate-introduces-ai-powered-features-in-sql-prompt/
[^12]: https://www.youtube.com/watch?v=Laj6jFiOzP0
[^13]: https://extensibility71.rssing.com/chan-5545477/article4336.html
[^14]: https://www.red-gate.com/products/sql-prompt/roadmap/
[^15]: https://dbforge-sql-complete.software.informer.com/4.8/```

