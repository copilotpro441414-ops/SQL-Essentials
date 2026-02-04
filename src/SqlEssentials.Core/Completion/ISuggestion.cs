using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    public interface ISuggestion
    {
        string DisplayText { get; }
        string InsertionText { get; }
        SuggestionType Type { get; }
        string Description { get; }
        string TypeInfo { get; }
        int RelevanceScore { get; }
        string FilterText { get; }
        string SortText { get; }
    }
}
