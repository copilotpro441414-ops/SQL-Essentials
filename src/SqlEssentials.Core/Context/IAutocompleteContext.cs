using System.Collections.Generic;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    public interface IAutocompleteContext
    {
        string QueryText { get; }
        int CursorPosition { get; }
        ClauseType CurrentClause { get; }
        string PartialInput { get; }
        string QualifierPrefix { get; }
        IReadOnlyDictionary<string, IAliasBinding> Aliases { get; }
        IReadOnlyList<string> ReferencedTables { get; }
        TriggerReason Trigger { get; }
    }
}
