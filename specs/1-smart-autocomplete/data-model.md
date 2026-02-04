# Data Model: SQL Essentials MVP

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-02-04  
**Source**: [spec.md](./spec.md) Key Entities section

## Overview

This document defines the core domain entities for the SQL Essentials MVP. All entities are designed as immutable records where possible, following C# best practices for .NET 4.8.

---

## 1. Metadata Entities

### DatabaseConnection

Represents an active connection to a SQL Server instance.

```csharp
public sealed class DatabaseConnection
{
    public string ServerName { get; }
    public string DatabaseName { get; }
    public string ConnectionKey { get; }  // "{ServerName}:{DatabaseName}"
    public AuthenticationMethod AuthMethod { get; }
    public bool IsConnected { get; }
}

public enum AuthenticationMethod
{
    WindowsIntegrated,
    SqlServerAuth,
    AzureAD
}
```

**Validation Rules**:
- `ServerName` and `DatabaseName` must not be null or empty
- `ConnectionKey` is derived, not set directly

**Relationships**:
- 1:1 with `SchemaCache` (one cache per connection)

---

### SchemaCache

Contains cached metadata for a specific database.

```csharp
public sealed class SchemaCache
{
    public string ConnectionKey { get; }
    public DateTimeOffset LastRefreshed { get; }
    public CacheStatus Status { get; }
    
    public IReadOnlyDictionary<string, TableDefinition> Tables { get; }
    public IReadOnlyDictionary<string, TableDefinition> Views { get; }
    public IReadOnlyList<ForeignKeyRelationship> ForeignKeys { get; }
    public IReadOnlyDictionary<string, ProcedureDefinition> Procedures { get; }
    public IReadOnlyDictionary<string, FunctionDefinition> Functions { get; }
    
    public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - LastRefreshed > ttl;
}

public enum CacheStatus
{
    Empty,
    Loading,
    Ready,
    Refreshing,
    Error
}
```

**Validation Rules**:
- `ConnectionKey` must match a valid `DatabaseConnection`
- `LastRefreshed` must be set when status is `Ready`

**State Transitions**:
```
Empty → Loading → Ready
Ready → Refreshing → Ready
Loading/Refreshing → Error (on failure)
```

---

### TableDefinition

Represents a table or view in the database.

```csharp
public sealed class TableDefinition
{
    public string SchemaName { get; }
    public string ObjectName { get; }
    public string FullyQualifiedName { get; }  // "[Schema].[Name]"
    public ObjectType Type { get; }  // Table or View
    public IReadOnlyList<ColumnDefinition> Columns { get; }
    
    public ColumnDefinition? GetColumn(string name) =>
        Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

public enum ObjectType
{
    Table,
    View
}
```

**Validation Rules**:
- `SchemaName` defaults to "dbo" if not specified
- `ObjectName` must not be null or empty
- `FullyQualifiedName` is derived with proper bracket escaping

---

### ColumnDefinition

Represents a column in a table or view.

```csharp
public sealed class ColumnDefinition
{
    public string Name { get; }
    public string DataType { get; }
    public bool IsNullable { get; }
    public bool IsPrimaryKey { get; }
    public bool IsForeignKey { get; }
    public bool IsIdentity { get; }
    public bool IsComputed { get; }
    public int? MaxLength { get; }  // For varchar, nvarchar, etc.
    public int? Precision { get; }  // For decimal, numeric
    public int? Scale { get; }      // For decimal, numeric
}
```

**Validation Rules**:
- `Name` must not be null or empty
- `DataType` must be a valid SQL Server type name

---

### ForeignKeyRelationship

Represents a foreign key constraint linking two tables.

```csharp
public sealed class ForeignKeyRelationship
{
    public string ConstraintName { get; }
    public string ParentSchema { get; }
    public string ParentTable { get; }
    public IReadOnlyList<string> ParentColumns { get; }
    public string ReferencedSchema { get; }
    public string ReferencedTable { get; }
    public IReadOnlyList<string> ReferencedColumns { get; }
    
    public string ParentFullName => $"[{ParentSchema}].[{ParentTable}]";
    public string ReferencedFullName => $"[{ReferencedSchema}].[{ReferencedTable}]";
}
```

**Validation Rules**:
- `ParentColumns` and `ReferencedColumns` must have matching counts
- All table/schema names must not be null or empty

---

### ProcedureDefinition

Represents a stored procedure.

```csharp
public sealed class ProcedureDefinition
{
    public string SchemaName { get; }
    public string ProcedureName { get; }
    public string FullyQualifiedName { get; }
    public IReadOnlyList<ParameterDefinition> Parameters { get; }
}
```

---

### FunctionDefinition

Represents a user-defined function.

```csharp
public sealed class FunctionDefinition
{
    public string SchemaName { get; }
    public string FunctionName { get; }
    public string FullyQualifiedName { get; }
    public FunctionType Type { get; }
    public string ReturnType { get; }
    public IReadOnlyList<ParameterDefinition> Parameters { get; }
}

public enum FunctionType
{
    Scalar,
    TableValued,
    InlineTableValued
}
```

---

### ParameterDefinition

Represents a parameter for procedures and functions.

```csharp
public sealed class ParameterDefinition
{
    public string Name { get; }  // Includes @ prefix
    public string DataType { get; }
    public ParameterDirection Direction { get; }
    public string? DefaultValue { get; }
}

public enum ParameterDirection
{
    Input,
    Output,
    InputOutput
}
```

---

## 2. Completion Entities

### AutocompleteContext

Represents the current query state for completion.

```csharp
public sealed class AutocompleteContext
{
    public string QueryText { get; }
    public int CursorPosition { get; }
    public ClauseType CurrentClause { get; }
    public string? PartialInput { get; }  // Text being typed (e.g., "u." or "Use")
    public IReadOnlyDictionary<string, AliasBinding> Aliases { get; }
    public IReadOnlyList<string> ReferencedTables { get; }
    public TriggerReason Trigger { get; }
}

public enum ClauseType
{
    Unknown,
    Select,
    From,
    Where,
    Join,
    On,
    GroupBy,
    Having,
    OrderBy,
    Insert,
    Update,
    Delete,
    Set,
    Values
}

public enum TriggerReason
{
    Explicit,      // User pressed Ctrl+Space
    DotAfterIdentifier,
    SpaceAfterKeyword,
    Typing
}
```

---

### AliasBinding

Maps an alias to its source table.

```csharp
public sealed class AliasBinding
{
    public string Alias { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    public string FullyQualifiedName { get; }
    public int DefinedAtPosition { get; }  // Position in query where alias was defined
}
```

---

### Suggestion

Represents an autocomplete candidate.

```csharp
public sealed class Suggestion
{
    public string DisplayText { get; }
    public string InsertionText { get; }
    public SuggestionType Type { get; }
    public string? Description { get; }
    public string? TypeInfo { get; }  // e.g., "int", "varchar(50)"
    public int RelevanceScore { get; }
    public ImageMoniker Icon { get; }
}

public enum SuggestionType
{
    Keyword,
    Table,
    View,
    Column,
    Procedure,
    Function,
    Snippet,
    Alias,
    JoinPredicate
}
```

**Ranking Rules** (FR-024):
1. Exact prefix match (+100)
2. Type relevance to clause (+50 for matching type)
3. Primary key column (+20)
4. Foreign key column (+15)
5. Usage frequency (learned, +0-10)

---

### JoinSuggestion

Specialized suggestion for JOIN predicates.

```csharp
public sealed class JoinSuggestion
{
    public string LeftAlias { get; }
    public string LeftTable { get; }
    public IReadOnlyList<string> LeftColumns { get; }
    public string RightAlias { get; }
    public string RightTable { get; }
    public IReadOnlyList<string> RightColumns { get; }
    public JoinMatchType MatchType { get; }
    public string PredicateText { get; }  // e.g., "u.UserID = o.UserID"
}

public enum JoinMatchType
{
    ForeignKey,      // Matched via FK constraint
    ColumnName,      // Matched via identical column names
    ColumnNameSimilar // Matched via similar names (e.g., UserID vs UserId)
}
```

---

## 3. Snippet Entities

### Snippet

Represents a code template.

```csharp
public sealed class Snippet
{
    public string Shortcut { get; }
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public IReadOnlyList<string> BodyLines { get; }
    public IReadOnlyDictionary<int, PlaceholderDefinition> Placeholders { get; }
    public bool IsBuiltIn { get; }
    
    public string ExpandedBody => string.Join(Environment.NewLine, BodyLines);
}
```

---

### PlaceholderDefinition

Defines a placeholder within a snippet.

```csharp
public sealed class PlaceholderDefinition
{
    public int Index { get; }  // $1, $2, etc.
    public string? DefaultValue { get; }
    public string? Description { get; }
}
```

---

### SnippetExpansionSession

Tracks active snippet expansion for Tab navigation.

```csharp
public sealed class SnippetExpansionSession
{
    public Snippet Snippet { get; }
    public int CurrentPlaceholderIndex { get; }
    public IReadOnlyList<PlaceholderSpan> PlaceholderSpans { get; }
    public bool IsComplete { get; }
    
    public PlaceholderSpan? CurrentSpan => 
        CurrentPlaceholderIndex < PlaceholderSpans.Count 
            ? PlaceholderSpans[CurrentPlaceholderIndex] 
            : null;
}

public sealed class PlaceholderSpan
{
    public int PlaceholderIndex { get; }
    public int StartPosition { get; }
    public int Length { get; }
}
```

---

## 4. Formatting Entities

### FormattingProfile

Configuration for SQL formatting rules.

```csharp
public sealed class FormattingProfile
{
    public string Name { get; }
    public KeywordCase KeywordCase { get; }
    public IndentStyle IndentStyle { get; }
    public int IndentSize { get; }
    public CommaPosition CommaPosition { get; }
    public bool JoinOnNewLine { get; }
    public bool OnPredicateIndent { get; }
    public bool SelectColumnsOnSeparateLines { get; }
    public int MaxLineLength { get; }
}

public enum KeywordCase { Upper, Lower, PascalCase }
public enum IndentStyle { Spaces, Tabs }
public enum CommaPosition { Trailing, Leading }
```

---

### FormattingResult

Output of a formatting operation.

```csharp
public sealed class FormattingResult
{
    public string FormattedText { get; }
    public bool Success { get; }
    public IReadOnlyList<FormattingError> Errors { get; }
}

public sealed class FormattingError
{
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
}
```

---

## Entity Relationship Diagram

```
┌─────────────────────┐         ┌─────────────────────┐
│ DatabaseConnection  │ 1 ─── 1 │    SchemaCache      │
└─────────────────────┘         └─────────────────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    ▼                    ▼
         ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
         │ TableDefinition │  │  ForeignKey     │  │ Procedure/Func  │
         │   (Table/View)  │  │  Relationship   │  │   Definition    │
         └─────────────────┘  └─────────────────┘  └─────────────────┘
                  │
                  │ 1:N
                  ▼
         ┌─────────────────┐
         │ColumnDefinition │
         └─────────────────┘

┌─────────────────────┐         ┌─────────────────────┐
│ AutocompleteContext │ ─────── │    Suggestion       │
└─────────────────────┘  1:N    └─────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────┐
│   AliasBinding      │
└─────────────────────┘

┌─────────────────────┐         ┌─────────────────────┐
│      Snippet        │ ─────── │PlaceholderDefinition│
└─────────────────────┘  1:N    └─────────────────────┘
```

---

**Phase 1 Data Model Complete**
