// Contract: ISnippetManager
// Purpose: Snippet management and expansion
// Source: FR-016 to FR-019 (Snippets)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlEssentials.Core.Contracts
{
    /// <summary>
    /// Manages snippet definitions and provides snippet expansion.
    /// FR-016: Built-in snippets for common patterns.
    /// FR-018: Settings UI to manage custom snippets.
    /// FR-019: Custom snippets with shortcut, body, placeholders.
    /// </summary>
    public interface ISnippetManager
    {
        /// <summary>
        /// Gets all available snippets (built-in + custom).
        /// </summary>
        IReadOnlyList<ISnippet> GetAllSnippets();

        /// <summary>
        /// Gets only built-in snippets.
        /// FR-016: Built-in snippets.
        /// </summary>
        IReadOnlyList<ISnippet> GetBuiltInSnippets();

        /// <summary>
        /// Gets only user-defined custom snippets.
        /// </summary>
        IReadOnlyList<ISnippet> GetCustomSnippets();

        /// <summary>
        /// Finds a snippet by its shortcut (case-insensitive).
        /// </summary>
        ISnippet? FindByShortcut(string shortcut);

        /// <summary>
        /// Finds snippets matching a prefix (for autocomplete).
        /// </summary>
        IReadOnlyList<ISnippet> FindByPrefix(string prefix);

        /// <summary>
        /// Adds a custom snippet.
        /// FR-018: Settings UI to add snippets.
        /// </summary>
        void AddCustomSnippet(ISnippet snippet);

        /// <summary>
        /// Updates an existing custom snippet.
        /// FR-018: Settings UI to edit snippets.
        /// </summary>
        void UpdateCustomSnippet(string shortcut, ISnippet updatedSnippet);

        /// <summary>
        /// Removes a custom snippet.
        /// FR-018: Settings UI to delete snippets.
        /// </summary>
        bool RemoveCustomSnippet(string shortcut);

        /// <summary>
        /// Saves custom snippets to persistent storage.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Reloads snippets from storage.
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// Exports snippets to JSON format.
        /// </summary>
        string ExportToJson(IEnumerable<ISnippet>? snippets = null);

        /// <summary>
        /// Imports snippets from JSON format.
        /// </summary>
        IReadOnlyList<ISnippet> ImportFromJson(string json);
    }

    /// <summary>
    /// A code snippet template.
    /// FR-019: Shortcut trigger, body text, placeholder definitions.
    /// </summary>
    public interface ISnippet
    {
        /// <summary>
        /// The shortcut trigger (e.g., "selj" for SELECT...JOIN).
        /// </summary>
        string Shortcut { get; }

        /// <summary>
        /// Display name for the snippet.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the snippet does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category for grouping (e.g., "DML", "DDL", "Flow Control").
        /// </summary>
        string Category { get; }

        /// <summary>
        /// The snippet body lines (may contain placeholders like $1, ${2:default}).
        /// </summary>
        IReadOnlyList<string> BodyLines { get; }

        /// <summary>
        /// Placeholder definitions keyed by index.
        /// </summary>
        IReadOnlyDictionary<int, IPlaceholderDefinition> Placeholders { get; }

        /// <summary>
        /// Whether this is a built-in snippet (not editable/deletable).
        /// </summary>
        bool IsBuiltIn { get; }

        /// <summary>
        /// Gets the full body as a single string with line breaks.
        /// </summary>
        string GetExpandedBody();
    }

    /// <summary>
    /// Definition for a placeholder within a snippet.
    /// </summary>
    public interface IPlaceholderDefinition
    {
        /// <summary>
        /// Placeholder index (1, 2, 3, etc.). $0 is the final cursor position.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Default value to insert if user doesn't modify.
        /// </summary>
        string? DefaultValue { get; }

        /// <summary>
        /// Description shown in UI when placeholder is active.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Possible values for autocomplete within this placeholder.
        /// </summary>
        IReadOnlyList<string>? Choices { get; }
    }

    /// <summary>
    /// Handles snippet expansion and placeholder navigation in the editor.
    /// FR-017: Placeholder navigation via Tab key.
    /// </summary>
    public interface ISnippetExpander
    {
        /// <summary>
        /// Expands a snippet at the specified position in the editor.
        /// </summary>
        /// <param name="snippet">The snippet to expand</param>
        /// <param name="insertPosition">Position to insert the snippet</param>
        /// <returns>An active expansion session for Tab navigation</returns>
        ISnippetSession Expand(ISnippet snippet, int insertPosition);

        /// <summary>
        /// Gets the currently active snippet session, if any.
        /// </summary>
        ISnippetSession? ActiveSession { get; }

        /// <summary>
        /// Cancels the current snippet session.
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Tracks an active snippet expansion for placeholder navigation.
    /// FR-017: Tab key navigation between placeholders.
    /// </summary>
    public interface ISnippetSession
    {
        /// <summary>
        /// The snippet being expanded.
        /// </summary>
        ISnippet Snippet { get; }

        /// <summary>
        /// Current placeholder index (0-based in navigation order).
        /// </summary>
        int CurrentPlaceholderIndex { get; }

        /// <summary>
        /// Total number of placeholders.
        /// </summary>
        int PlaceholderCount { get; }

        /// <summary>
        /// Whether all placeholders have been visited.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Moves to the next placeholder.
        /// FR-017: Tab key moves to next placeholder.
        /// </summary>
        /// <returns>True if moved to next; false if session complete</returns>
        bool MoveNext();

        /// <summary>
        /// Moves to the previous placeholder.
        /// </summary>
        /// <returns>True if moved; false if at first placeholder</returns>
        bool MovePrevious();

        /// <summary>
        /// Gets the current placeholder span in the document.
        /// </summary>
        IPlaceholderSpan? GetCurrentSpan();

        /// <summary>
        /// Completes the session and positions cursor at $0 or end.
        /// </summary>
        void Complete();
    }

    /// <summary>
    /// Represents a placeholder's position in the expanded snippet.
    /// </summary>
    public interface IPlaceholderSpan
    {
        int PlaceholderIndex { get; }
        int StartPosition { get; }
        int Length { get; }
        string CurrentText { get; }
    }
}
