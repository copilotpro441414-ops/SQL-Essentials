using System;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    /// <summary>
    /// Policy for calculating suggestion relevance scores based on clause context and user input.
    /// Isolated component for deterministic scoring logic without completion engine dependencies.
    /// </summary>
    public sealed class SuggestionScoringPolicy
    {
        /// <summary>
        /// Calculates the total relevance score for a suggestion.
        /// </summary>
        /// <param name="suggestion">The suggestion to score</param>
        /// <param name="clause">The current SQL clause context</param>
        /// <param name="typedToken">The partial token the user has typed</param>
        /// <returns>Total score combining base relevance, clause bonus, and prefix match bonus</returns>
        public int CalculateScore(ISuggestion suggestion, ClauseType clause, string typedToken)
        {
            if (suggestion == null)
            {
                throw new ArgumentNullException(nameof(suggestion));
            }

            var score = suggestion.RelevanceScore;
            score += GetClauseBonus(suggestion.Type, clause);

            if (!string.IsNullOrEmpty(typedToken) &&
                suggestion.DisplayText.StartsWith(typedToken, StringComparison.OrdinalIgnoreCase))
            {
                score += 50; // Prefix match bonus
            }

            return score;
        }

        /// <summary>
        /// Gets the clause-specific bonus score for a suggestion type.
        /// </summary>
        /// <param name="type">The type of suggestion (table, column, etc.)</param>
        /// <param name="clause">The current SQL clause</param>
        /// <returns>Bonus score (typically 0 or 40)</returns>
        public int GetClauseBonus(SuggestionType type, ClauseType clause)
        {
            switch (clause)
            {
                case ClauseType.From:
                case ClauseType.Join:
                    // In FROM/JOIN clauses, prioritize tables and views
                    return type == SuggestionType.Table || type == SuggestionType.View ? 40 : 0;

                case ClauseType.Select:
                case ClauseType.Where:
                case ClauseType.On:
                case ClauseType.GroupBy:
                case ClauseType.OrderBy:
                case ClauseType.Having:
                    // In data manipulation clauses, prioritize columns
                    return type == SuggestionType.Column ? 40 : 0;

                default:
                    return 0;
            }
        }
    }
}
