using System.Collections.Generic;

namespace SqlEssentials.Core.Context
{
    public interface IContextAnalyzer
    {
        IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null);

        IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText);

        bool IsInNonCodeContext(string queryText, int cursorPosition);
    }
}
