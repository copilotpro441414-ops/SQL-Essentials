using System;
using SqlEssentials.Core.Formatting;

namespace SqlEssentials.Core.Models
{
    public sealed class FormattingProfile : IFormattingProfile
    {
        public FormattingProfile(
            string name,
            KeywordCase keywordCase,
            IndentStyle indentStyle,
            int indentSize,
            CommaPosition commaPosition,
            bool joinOnNewLine,
            bool indentOnPredicate,
            bool selectColumnsOnSeparateLines,
            int maxLineLength,
            bool blankLineBeforeClauses,
            bool alignEquals)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Default" : name;
            KeywordCase = keywordCase;
            IndentStyle = indentStyle;
            IndentSize = Math.Max(1, indentSize);
            CommaPosition = commaPosition;
            JoinOnNewLine = joinOnNewLine;
            IndentOnPredicate = indentOnPredicate;
            SelectColumnsOnSeparateLines = selectColumnsOnSeparateLines;
            MaxLineLength = Math.Max(0, maxLineLength);
            BlankLineBeforeClauses = blankLineBeforeClauses;
            AlignEquals = alignEquals;
        }

        public string Name { get; }
        public KeywordCase KeywordCase { get; }
        public IndentStyle IndentStyle { get; }
        public int IndentSize { get; }
        public CommaPosition CommaPosition { get; }
        public bool JoinOnNewLine { get; }
        public bool IndentOnPredicate { get; }
        public bool SelectColumnsOnSeparateLines { get; }
        public int MaxLineLength { get; }
        public bool BlankLineBeforeClauses { get; }
        public bool AlignEquals { get; }
    }
}
