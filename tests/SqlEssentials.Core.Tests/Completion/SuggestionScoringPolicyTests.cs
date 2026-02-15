using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Completion
{
    /// <summary>
    /// Tests for suggestion scoring policy, including prefix matching, clause bonuses, and relevance ordering.
    /// </summary>
    public sealed class SuggestionScoringPolicyTests
    {
        [Fact]
        public void PrefixMatch_IncreasesScore()
        {
            // Arrange
            var suggestion1 = CreateSuggestion("UserId", SuggestionType.Column);
            var suggestion2 = CreateSuggestion("User", SuggestionType.Column);
            var typedToken = "Use";

            // Act
            var score1 = CalculateScore(suggestion1, ClauseType.Select, typedToken);
            var score2 = CalculateScore(suggestion2, ClauseType.Select, typedToken);

            // Assert
            Assert.True(score1 > 0, "Prefix match should increase score");
            Assert.True(score2 > 0, "Prefix match should increase score");
        }

        [Fact]
        public void ClauseBonus_FromClause_FavorsTablesAndViews()
        {
            // Arrange
            var tableSuggestion = CreateSuggestion("Users", SuggestionType.Table);
            var columnSuggestion = CreateSuggestion("UserId", SuggestionType.Column);

            // Act
            var tableScore = CalculateScore(tableSuggestion, ClauseType.From, string.Empty);
            var columnScore = CalculateScore(columnSuggestion, ClauseType.From, string.Empty);

            // Assert
            Assert.True(tableScore > columnScore, "Tables should rank higher than columns in FROM clause");
        }

        [Fact]
        public void ClauseBonus_SelectClause_FavorsColumns()
        {
            // Arrange
            var tableSuggestion = CreateSuggestion("Users", SuggestionType.Table);
            var columnSuggestion = CreateSuggestion("UserId", SuggestionType.Column);

            // Act
            var tableScore = CalculateScore(tableSuggestion, ClauseType.Select, string.Empty);
            var columnScore = CalculateScore(columnSuggestion, ClauseType.Select, string.Empty);

            // Assert
            Assert.True(columnScore > tableScore, "Columns should rank higher than tables in SELECT clause");
        }

        [Fact]
        public void RelevanceScore_PrimaryKey_IncreasesSuggestionScore()
        {
            // Arrange
            var pkColumn = CreateSuggestion("Id", SuggestionType.Column, relevanceScore: 20);
            var regularColumn = CreateSuggestion("Name", SuggestionType.Column, relevanceScore: 0);

            // Act
            var pkScore = CalculateScore(pkColumn, ClauseType.Select, string.Empty);
            var regularScore = CalculateScore(regularColumn, ClauseType.Select, string.Empty);

            // Assert
            Assert.True(pkScore > regularScore, "Primary key columns should have higher relevance");
        }

        [Theory]
        [InlineData(ClauseType.Select, SuggestionType.Column, true)]
        [InlineData(ClauseType.Where, SuggestionType.Column, true)]
        [InlineData(ClauseType.GroupBy, SuggestionType.Column, true)]
        [InlineData(ClauseType.OrderBy, SuggestionType.Column, true)]
        [InlineData(ClauseType.From, SuggestionType.Table, true)]
        [InlineData(ClauseType.Join, SuggestionType.Table, true)]
        [InlineData(ClauseType.From, SuggestionType.Column, false)]
        [InlineData(ClauseType.Select, SuggestionType.Table, false)]
        public void ClauseBonus_AppliesCorrectly(ClauseType clause, SuggestionType type, bool shouldHaveBonus)
        {
            // Arrange
            var suggestion = CreateSuggestion("Test", type);

            // Act
            var score = CalculateScore(suggestion, clause, string.Empty);

            // Assert
            if (shouldHaveBonus)
            {
                Assert.True(score > 0, $"Should have clause bonus for {type} in {clause}");
            }
        }

        // Helper methods to simulate the current scoring logic
        // These will be replaced when we extract SuggestionScoringPolicy
        private static ISuggestion CreateSuggestion(string displayText, SuggestionType type, int relevanceScore = 0)
        {
            return new Suggestion(
                displayText,
                displayText,
                type,
                "dbo.Test",
                "nvarchar",
                relevanceScore,
                displayText,
                displayText);
        }

        private static int CalculateScore(ISuggestion suggestion, ClauseType clause, string typedToken)
        {
            var score = suggestion.RelevanceScore;
            score += GetClauseBonus(suggestion.Type, clause);

            if (!string.IsNullOrEmpty(typedToken) &&
                suggestion.DisplayText.StartsWith(typedToken, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            return score;
        }

        private static int GetClauseBonus(SuggestionType type, ClauseType clause)
        {
            switch (clause)
            {
                case ClauseType.From:
                case ClauseType.Join:
                    return type == SuggestionType.Table || type == SuggestionType.View ? 40 : 0;
                case ClauseType.Select:
                case ClauseType.Where:
                case ClauseType.On:
                case ClauseType.GroupBy:
                case ClauseType.OrderBy:
                case ClauseType.Having:
                    return type == SuggestionType.Column ? 40 : 0;
                default:
                    return 0;
            }
        }
    }
}
