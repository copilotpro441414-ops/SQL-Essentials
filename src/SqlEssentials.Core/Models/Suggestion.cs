using SqlEssentials.Core.Completion;

namespace SqlEssentials.Core.Models
{
    public sealed class Suggestion : ISuggestion
    {
        public Suggestion(
            string displayText,
            string insertionText,
            SuggestionType type,
            string description,
            string typeInfo,
            int relevanceScore,
            string filterText,
            string sortText)
        {
            DisplayText = displayText;
            InsertionText = insertionText;
            Type = type;
            Description = description;
            TypeInfo = typeInfo;
            RelevanceScore = relevanceScore;
            FilterText = filterText;
            SortText = sortText;
        }

        public string DisplayText { get; }
        public string InsertionText { get; }
        public SuggestionType Type { get; }
        public string Description { get; }
        public string TypeInfo { get; }
        public int RelevanceScore { get; }
        public string FilterText { get; }
        public string SortText { get; }
    }
}
