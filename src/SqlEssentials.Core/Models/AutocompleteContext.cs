using System;
using System.Collections.Generic;
using SqlEssentials.Core.Context;

namespace SqlEssentials.Core.Models
{
    public sealed class AutocompleteContext : IAutocompleteContext
    {
        public AutocompleteContext(
            string queryText,
            int cursorPosition,
            ClauseType currentClause,
            string partialInput,
            string qualifierPrefix,
            IReadOnlyDictionary<string, IAliasBinding> aliases,
            IReadOnlyList<string> referencedTables,
            TriggerReason trigger)
        {
            QueryText = queryText ?? throw new ArgumentNullException(nameof(queryText));
            CursorPosition = cursorPosition;
            CurrentClause = currentClause;
            PartialInput = partialInput;
            QualifierPrefix = qualifierPrefix;
            Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
            ReferencedTables = referencedTables ?? throw new ArgumentNullException(nameof(referencedTables));
            Trigger = trigger;
        }

        public string QueryText { get; }
        public int CursorPosition { get; }
        public ClauseType CurrentClause { get; }
        public string PartialInput { get; }
        public string QualifierPrefix { get; }
        public IReadOnlyDictionary<string, IAliasBinding> Aliases { get; }
        public IReadOnlyList<string> ReferencedTables { get; }
        public TriggerReason Trigger { get; }
    }
}
