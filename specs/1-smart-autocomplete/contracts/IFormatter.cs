// Contract: IFormatter
// Purpose: SQL formatting engine
// Source: FR-020 to FR-023 (Formatting)

using System.Collections.Generic;

namespace SqlEssentials.Core.Contracts
{
    /// <summary>
    /// Formats SQL code according to a formatting profile.
    /// FR-020: Format Document command.
    /// FR-021: Format Selection command.
    /// FR-023: Preserve semantic meaning.
    /// </summary>
    public interface ISqlFormatter
    {
        /// <summary>
        /// Formats the entire SQL text.
        /// FR-020: Format Document.
        /// </summary>
        /// <param name="sql">The SQL text to format</param>
        /// <param name="profile">Formatting rules to apply (null = default profile)</param>
        /// <returns>Formatting result with formatted text or errors</returns>
        IFormattingResult Format(string sql, IFormattingProfile? profile = null);

        /// <summary>
        /// Formats a selection within SQL text.
        /// FR-021: Format Selection.
        /// </summary>
        /// <param name="sql">The full SQL text</param>
        /// <param name="selectionStart">Start position of selection</param>
        /// <param name="selectionLength">Length of selection</param>
        /// <param name="profile">Formatting rules to apply (null = default profile)</param>
        /// <returns>Formatting result with the text to replace the selection</returns>
        IFormattingResult FormatSelection(string sql, int selectionStart, int selectionLength, IFormattingProfile? profile = null);

        /// <summary>
        /// Gets the default formatting profile.
        /// </summary>
        IFormattingProfile DefaultProfile { get; }
    }

    /// <summary>
    /// Result of a formatting operation.
    /// </summary>
    public interface IFormattingResult
    {
        /// <summary>
        /// The formatted SQL text (or original if formatting failed).
        /// </summary>
        string FormattedText { get; }

        /// <summary>
        /// Whether formatting succeeded.
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Errors encountered during formatting (e.g., parse errors).
        /// </summary>
        IReadOnlyList<IFormattingError> Errors { get; }

        /// <summary>
        /// Warnings that don't prevent formatting but indicate issues.
        /// </summary>
        IReadOnlyList<IFormattingError> Warnings { get; }
    }

    /// <summary>
    /// An error or warning from the formatting process.
    /// </summary>
    public interface IFormattingError
    {
        int Line { get; }
        int Column { get; }
        string Message { get; }
        FormattingErrorSeverity Severity { get; }
    }

    public enum FormattingErrorSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// Formatting rules configuration.
    /// FR-022: Default formatting profile with configurable options.
    /// </summary>
    public interface IFormattingProfile
    {
        /// <summary>
        /// Profile name for identification.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// How to case SQL keywords (SELECT, FROM, etc.).
        /// Default: Upper.
        /// </summary>
        KeywordCase KeywordCase { get; }

        /// <summary>
        /// Use spaces or tabs for indentation.
        /// Default: Spaces.
        /// </summary>
        IndentStyle IndentStyle { get; }

        /// <summary>
        /// Number of spaces/tabs per indent level.
        /// Default: 4.
        /// </summary>
        int IndentSize { get; }

        /// <summary>
        /// Comma position in column lists.
        /// Default: Trailing.
        /// </summary>
        CommaPosition CommaPosition { get; }

        /// <summary>
        /// Place each JOIN clause on a new line.
        /// FR-022: JOINs on new lines.
        /// Default: true.
        /// </summary>
        bool JoinOnNewLine { get; }

        /// <summary>
        /// Indent ON predicates relative to JOIN.
        /// FR-022: ON predicates indented.
        /// Default: true.
        /// </summary>
        bool IndentOnPredicate { get; }

        /// <summary>
        /// Place each SELECT column on a separate line.
        /// Default: false (only for long lists).
        /// </summary>
        bool SelectColumnsOnSeparateLines { get; }

        /// <summary>
        /// Maximum line length before wrapping (0 = no limit).
        /// Default: 120.
        /// </summary>
        int MaxLineLength { get; }

        /// <summary>
        /// Add blank line before major clauses (FROM, WHERE, etc.).
        /// Default: false.
        /// </summary>
        bool BlankLineBeforeClauses { get; }

        /// <summary>
        /// Align equals signs in SET clauses and column aliases.
        /// Default: false.
        /// </summary>
        bool AlignEquals { get; }
    }

    public enum KeywordCase
    {
        /// <summary>UPPERCASE keywords (SELECT, FROM, WHERE).</summary>
        Upper,

        /// <summary>lowercase keywords (select, from, where).</summary>
        Lower,

        /// <summary>PascalCase keywords (Select, From, Where).</summary>
        PascalCase
    }

    public enum IndentStyle
    {
        /// <summary>Use spaces for indentation.</summary>
        Spaces,

        /// <summary>Use tabs for indentation.</summary>
        Tabs
    }

    public enum CommaPosition
    {
        /// <summary>Comma at end of line: "column1,\n column2".</summary>
        Trailing,

        /// <summary>Comma at start of line: "column1\n,column2".</summary>
        Leading
    }
}
