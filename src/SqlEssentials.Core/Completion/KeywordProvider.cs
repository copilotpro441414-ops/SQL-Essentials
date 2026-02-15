using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    public sealed class KeywordProvider
    {
        private static readonly string[] Keywords =
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "ON",
            "GROUP BY", "ORDER BY", "HAVING", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
            "DELETE", "WITH", "AS", "DISTINCT", "TOP", "AND", "OR", "NOT", "NULL",
            "IN", "EXISTS", "CASE", "WHEN", "THEN", "ELSE", "END", "TRY", "CATCH"
        };

        public IReadOnlyList<ISuggestion> GetSuggestions(string partialInput)
        {
            IEnumerable<string> candidates = Keywords;
            if (!string.IsNullOrWhiteSpace(partialInput))
            {
                candidates = candidates.Where(k => k.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase));
            }

            return candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Select(k => (ISuggestion)new Suggestion(
                    k,
                    k,
                    SuggestionType.Keyword,
                    "T-SQL keyword",
                    string.Empty,
                    relevanceScore: 0,
                    filterText: k,
                    sortText: k))
                .ToList();
        }
    }
}