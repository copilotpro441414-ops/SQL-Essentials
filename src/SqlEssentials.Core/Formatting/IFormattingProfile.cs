using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Formatting
{
    public interface IFormattingProfile
    {
        string Name { get; }
        KeywordCase KeywordCase { get; }
        IndentStyle IndentStyle { get; }
        int IndentSize { get; }
        CommaPosition CommaPosition { get; }
        bool JoinOnNewLine { get; }
        bool IndentOnPredicate { get; }
        bool SelectColumnsOnSeparateLines { get; }
        int MaxLineLength { get; }
        bool BlankLineBeforeClauses { get; }
        bool AlignEquals { get; }
    }
}
