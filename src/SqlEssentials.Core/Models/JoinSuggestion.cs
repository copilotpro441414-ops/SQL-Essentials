using System;
using System.Collections.Generic;
using SqlEssentials.Core.Completion;

namespace SqlEssentials.Core.Models
{
    public sealed class JoinSuggestion : ISuggestion
    {
        public JoinSuggestion(
            string displayText,
            string insertionText,
            string description,
            int relevanceScore,
            string leftAlias,
            string leftTable,
            IReadOnlyList<string> leftColumns,
            string rightAlias,
            string rightTable,
            IReadOnlyList<string> rightColumns,
            JoinMatchType matchType)
        {
            DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
            InsertionText = insertionText ?? throw new ArgumentNullException(nameof(insertionText));
            Description = description ?? string.Empty;
            RelevanceScore = relevanceScore;
            LeftAlias = leftAlias ?? throw new ArgumentNullException(nameof(leftAlias));
            LeftTable = leftTable ?? throw new ArgumentNullException(nameof(leftTable));
            LeftColumns = leftColumns ?? throw new ArgumentNullException(nameof(leftColumns));
            RightAlias = rightAlias ?? throw new ArgumentNullException(nameof(rightAlias));
            RightTable = rightTable ?? throw new ArgumentNullException(nameof(rightTable));
            RightColumns = rightColumns ?? throw new ArgumentNullException(nameof(rightColumns));
            MatchType = matchType;
        }

        public string DisplayText { get; }
        public string InsertionText { get; }
        public SuggestionType Type => SuggestionType.JoinPredicate;
        public string Description { get; }
        public string TypeInfo => string.Empty;
        public int RelevanceScore { get; }
        public string FilterText => DisplayText;
        public string SortText => DisplayText;

        public string LeftAlias { get; }
        public string LeftTable { get; }
        public IReadOnlyList<string> LeftColumns { get; }
        public string RightAlias { get; }
        public string RightTable { get; }
        public IReadOnlyList<string> RightColumns { get; }
        public JoinMatchType MatchType { get; }
    }
}