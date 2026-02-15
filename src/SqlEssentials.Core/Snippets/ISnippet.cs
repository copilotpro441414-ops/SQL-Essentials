using System.Collections.Generic;

namespace SqlEssentials.Core.Snippets
{
    public interface ISnippet
    {
        string Shortcut { get; }
        string Name { get; }
        string Description { get; }
        string Category { get; }
        IReadOnlyList<string> BodyLines { get; }
        IReadOnlyDictionary<int, IPlaceholderDefinition> Placeholders { get; }
        bool IsBuiltIn { get; }
        string ExpandedBody { get; }
    }

    public interface IPlaceholderDefinition
    {
        int Index { get; }
        string DefaultValue { get; }
        string Description { get; }
    }
}