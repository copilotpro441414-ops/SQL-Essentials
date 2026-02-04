using System.Collections.Generic;

namespace SqlEssentials.Core.Context
{
    public interface IContextAnalyzer
    {
        IAutocompleteContext Analyze(string queryText, int cursorPosition);

        IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText);

        bool IsInNonCodeContext(string queryText, int cursorPosition);
    }
}
