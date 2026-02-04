// Contract: ICompletionProvider
// Purpose: Generate autocomplete suggestions
// Source: FR-001 to FR-005 (Autocomplete Core), FR-009 to FR-011 (JOIN Assistance), FR-024 (Ranking)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqlEssentials.Core.Contracts
{
    /// <summary>
    /// Main entry point for generating autocomplete suggestions.
    /// Coordinates context analysis, metadata lookup, and suggestion ranking.
    /// </summary>
    public interface ICompletionEngine
    {
        /// <summary>
        /// Generates ranked suggestions for the given query and cursor position.
        /// FR-001: Must complete within 100ms of trigger keystroke.
        /// </summary>
        /// <param name="queryText">The full SQL query text</param>
        /// <param name="cursorPosition">0-based cursor position</param>
        /// <param name="connectionKey">Database connection key for metadata lookup</param>
        /// <param name="trigger">What triggered the completion request</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Ordered list of suggestions (highest relevance first)</returns>
        Task<ICompletionResult> GetCompletionsAsync(
            string queryText,
            int cursorPosition,
            string connectionKey,
            TriggerReason trigger,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a completion request.
    /// </summary>
    public interface ICompletionResult
    {
        /// <summary>
        /// Ordered list of suggestions (highest relevance first).
        /// </summary>
        IReadOnlyList<ISuggestion> Suggestions { get; }

        /// <summary>
        /// The span in the original text that should be replaced by the selected suggestion.
        /// </summary>
        TextSpan ApplicableSpan { get; }

        /// <summary>
        /// Whether the completion list should filter as user types.
        /// FR-003: Filter suggestions by prefix as user types.
        /// </summary>
        bool ShouldFilter { get; }

        /// <summary>
        /// Pre-selected suggestion index, or -1 for no pre-selection.
        /// </summary>
        int PreselectedIndex { get; }
    }

    /// <summary>
    /// A single autocomplete suggestion.
    /// </summary>
    public interface ISuggestion
    {
        /// <summary>
        /// Text shown in the completion popup.
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// Text inserted when suggestion is selected.
        /// May differ from DisplayText (e.g., brackets for special names).
        /// </summary>
        string InsertionText { get; }

        /// <summary>
        /// Category of the suggestion.
        /// </summary>
        SuggestionType Type { get; }

        /// <summary>
        /// Optional description shown in tooltip.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Type information (e.g., "int", "varchar(50)") for columns.
        /// </summary>
        string? TypeInfo { get; }

        /// <summary>
        /// Relevance score for ranking. Higher = more relevant.
        /// FR-024: Ranking algorithm.
        /// </summary>
        int RelevanceScore { get; }

        /// <summary>
        /// Filter text for prefix matching (usually same as DisplayText).
        /// </summary>
        string FilterText { get; }

        /// <summary>
        /// Sort text for ordering (usually same as DisplayText).
        /// </summary>
        string SortText { get; }
    }

    /// <summary>
    /// Provider for a specific type of suggestion (table, column, keyword, etc.).
    /// Multiple providers are composed by the completion engine.
    /// </summary>
    public interface ICompletionProvider
    {
        /// <summary>
        /// The type of suggestions this provider generates.
        /// </summary>
        SuggestionType SuggestionType { get; }

        /// <summary>
        /// Determines if this provider should contribute suggestions for the given context.
        /// </summary>
        bool ShouldProvide(IAutocompleteContext context);

        /// <summary>
        /// Generates suggestions for the given context.
        /// </summary>
        Task<IReadOnlyList<ISuggestion>> GetSuggestionsAsync(
            IAutocompleteContext context,
            IDatabaseCache? cache,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Specialized provider for JOIN predicate suggestions.
    /// FR-009 to FR-011: JOIN assistance.
    /// </summary>
    public interface IJoinPredicateProvider
    {
        /// <summary>
        /// Generates JOIN predicate suggestions based on FK relationships and column name matching.
        /// FR-009: Suggest based on FK relationships.
        /// FR-010: Fall back to column name matching.
        /// FR-011: Rank FK-based suggestions higher.
        /// </summary>
        Task<IReadOnlyList<IJoinSuggestion>> GetJoinSuggestionsAsync(
            IJoinContext joinContext,
            IDatabaseCache cache,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A JOIN predicate suggestion.
    /// </summary>
    public interface IJoinSuggestion : ISuggestion
    {
        string LeftAlias { get; }
        string LeftTable { get; }
        IReadOnlyList<string> LeftColumns { get; }
        string RightAlias { get; }
        string RightTable { get; }
        IReadOnlyList<string> RightColumns { get; }
        JoinMatchType MatchType { get; }
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
        JoinPredicate,
        Schema,
        Database
    }

    public enum JoinMatchType
    {
        /// <summary>Matched via FK constraint (highest confidence).</summary>
        ForeignKey,

        /// <summary>Matched via identical column names.</summary>
        ColumnNameExact,

        /// <summary>Matched via similar column names (e.g., UserID vs UserId).</summary>
        ColumnNameSimilar
    }

    public readonly struct TextSpan
    {
        public int Start { get; }
        public int Length { get; }

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int End => Start + Length;
    }
}
