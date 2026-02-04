// Contract: IContextAnalyzer
// Purpose: Analyze SQL query context for autocomplete
// Source: FR-006 to FR-008 (Context Awareness)

using System.Collections.Generic;

namespace SqlEssentials.Core.Contracts
{
    /// <summary>
    /// Analyzes SQL query text to determine context for autocomplete suggestions.
    /// FR-006: Detect current SQL clause and prioritize suggestions accordingly.
    /// FR-007: Track table aliases defined in the current query.
    /// </summary>
    public interface IContextAnalyzer
    {
        /// <summary>
        /// Analyzes the query text and cursor position to build autocomplete context.
        /// </summary>
        /// <param name="queryText">The full SQL query text</param>
        /// <param name="cursorPosition">0-based cursor position in the query</param>
        /// <returns>Context information for generating suggestions</returns>
        IAutocompleteContext Analyze(string queryText, int cursorPosition);

        /// <summary>
        /// Extracts all table aliases defined in a query.
        /// </summary>
        IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText);

        /// <summary>
        /// Determines if the cursor is inside a string literal or comment.
        /// Autocomplete should not trigger in these contexts.
        /// </summary>
        bool IsInNonCodeContext(string queryText, int cursorPosition);
    }

    /// <summary>
    /// Represents the analyzed context at a cursor position.
    /// </summary>
    public interface IAutocompleteContext
    {
        /// <summary>
        /// The full query text being analyzed.
        /// </summary>
        string QueryText { get; }

        /// <summary>
        /// The 0-based cursor position.
        /// </summary>
        int CursorPosition { get; }

        /// <summary>
        /// The SQL clause containing the cursor.
        /// FR-006: Detect current SQL clause.
        /// </summary>
        ClauseType CurrentClause { get; }

        /// <summary>
        /// The partial text being typed at the cursor (e.g., "u." or "Use").
        /// Used for prefix filtering.
        /// </summary>
        string? PartialInput { get; }

        /// <summary>
        /// If PartialInput contains a dot, this is the identifier before the dot.
        /// Used for alias resolution (FR-008).
        /// </summary>
        string? QualifierPrefix { get; }

        /// <summary>
        /// All table aliases defined in the query.
        /// FR-007: Track table aliases.
        /// </summary>
        IReadOnlyDictionary<string, IAliasBinding> Aliases { get; }

        /// <summary>
        /// Tables explicitly referenced in the query (for unaliased references).
        /// </summary>
        IReadOnlyList<string> ReferencedTables { get; }

        /// <summary>
        /// What triggered the autocomplete request.
        /// </summary>
        TriggerReason Trigger { get; }

        /// <summary>
        /// For JOIN ON context, the tables being joined.
        /// </summary>
        IJoinContext? JoinContext { get; }
    }

    /// <summary>
    /// Binds an alias to its source table.
    /// </summary>
    public interface IAliasBinding
    {
        string Alias { get; }
        string SchemaName { get; }
        string TableName { get; }
        string FullyQualifiedName { get; }
    }

    /// <summary>
    /// Context for JOIN ON clause suggestions.
    /// </summary>
    public interface IJoinContext
    {
        /// <summary>
        /// The table/alias on the left side of the JOIN.
        /// </summary>
        string LeftTableOrAlias { get; }

        /// <summary>
        /// The table/alias on the right side of the JOIN (the one being joined).
        /// </summary>
        string RightTableOrAlias { get; }

        /// <summary>
        /// Resolved left table name (if alias was used).
        /// </summary>
        string? ResolvedLeftTable { get; }

        /// <summary>
        /// Resolved right table name (if alias was used).
        /// </summary>
        string? ResolvedRightTable { get; }
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
        Values,
        Into,
        Exec,
        With  // CTE
    }

    public enum TriggerReason
    {
        /// <summary>User explicitly triggered autocomplete (Ctrl+Space).</summary>
        Explicit,

        /// <summary>User typed a dot after an identifier (alias.column).</summary>
        DotAfterIdentifier,

        /// <summary>User typed a space after a keyword (SELECT, FROM, etc.).</summary>
        SpaceAfterKeyword,

        /// <summary>User is typing characters (implicit trigger).</summary>
        Typing
    }
}
