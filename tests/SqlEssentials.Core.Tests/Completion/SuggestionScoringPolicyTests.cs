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
    /// Tests the extracted SuggestionScoringPolicy component directly.
    /// </summary>
    public sealed class SuggestionScoringPolicyTests
    {
        private readonly SuggestionScoringPolicy _policy = new SuggestionScoringPolicy();

        [Fact]
        public void PrefixMatch_IncreasesScore()
        {
            // Arrange
            var suggestion1 = CreateSuggestion("UserId", SuggestionType.Column);
            var suggestion2 = CreateSuggestion("User", SuggestionType.Column);
            var typedToken = "Use";

            // Act
            var score1 = _policy.CalculateScore(suggestion1, ClauseType.Select, typedToken);
            var score2 = _policy.CalculateScore(suggestion2, ClauseType.Select, typedToken);

            // Assert
            Assert.True(score1 > 0, "Prefix match should increase score");
            Assert.True(score2 > 0, "Prefix match should increase score");
            Assert.Equal(50, score1 - suggestion1.RelevanceScore - _policy.GetClauseBonus(SuggestionType.Column, ClauseType.Select));
        }

        [Fact]
        public void ClauseBonus_FromClause_FavorsTablesAndViews()
        {
            // Arrange
            var tableSuggestion = CreateSuggestion("Users", SuggestionType.Table);
            var columnSuggestion = CreateSuggestion("UserId", SuggestionType.Column);

            // Act
            var tableScore = _policy.CalculateScore(tableSuggestion, ClauseType.From, string.Empty);
            var columnScore = _policy.CalculateScore(columnSuggestion, ClauseType.From, string.Empty);

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
            var tableScore = _policy.CalculateScore(tableSuggestion, ClauseType.Select, string.Empty);
            var columnScore = _policy.CalculateScore(columnSuggestion, ClauseType.Select, string.Empty);

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
            var pkScore = _policy.CalculateScore(pkColumn, ClauseType.Select, string.Empty);
            var regularScore = _policy.CalculateScore(regularColumn, ClauseType.Select, string.Empty);

            // Assert
            Assert.True(pkScore > regularScore, "Primary key columns should have higher relevance");
            Assert.Equal(20, pkScore - regularScore);
        }

        [Theory]
        [InlineData(ClauseType.Select, SuggestionType.Column, 40)]
        [InlineData(ClauseType.Where, SuggestionType.Column, 40)]
        [InlineData(ClauseType.GroupBy, SuggestionType.Column, 40)]
        [InlineData(ClauseType.OrderBy, SuggestionType.Column, 40)]
        [InlineData(ClauseType.On, SuggestionType.Column, 40)]
        [InlineData(ClauseType.Having, SuggestionType.Column, 40)]
        [InlineData(ClauseType.From, SuggestionType.Table, 40)]
        [InlineData(ClauseType.Join, SuggestionType.Table, 40)]
        [InlineData(ClauseType.From, SuggestionType.View, 40)]
        [InlineData(ClauseType.Join, SuggestionType.View, 40)]
        [InlineData(ClauseType.From, SuggestionType.Column, 0)]
        [InlineData(ClauseType.Select, SuggestionType.Table, 0)]
        [InlineData(ClauseType.Unknown, SuggestionType.Column, 0)]
        [InlineData(ClauseType.Unknown, SuggestionType.Table, 0)]
        public void GetClauseBonus_ReturnsCorrectValue(ClauseType clause, SuggestionType type, int expectedBonus)
        {
            // Act
            var bonus = _policy.GetClauseBonus(type, clause);

            // Assert
            Assert.Equal(expectedBonus, bonus);
        }

        [Fact]
        public void CalculateScore_NullSuggestion_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _policy.CalculateScore(null, ClauseType.Select, "test"));
        }

        [Fact]
        public void CalculateScore_CombinesAllComponents()
        {
            // Arrange
            var suggestion = CreateSuggestion("UserId", SuggestionType.Column, relevanceScore: 20);
            var typedToken = "Use";

            // Act
            var score = _policy.CalculateScore(suggestion, ClauseType.Select, typedToken);

            // Assert
            // Score should be: 20 (base) + 40 (clause bonus) + 50 (prefix match) = 110
            Assert.Equal(110, score);
        }

        [Fact]
        public void PrefixMatch_CaseInsensitive()
        {
            // Arrange
            var suggestion = CreateSuggestion("UserId", SuggestionType.Column);

            // Act
            var scoreUpper = _policy.CalculateScore(suggestion, ClauseType.Select, "USE");
            var scoreLower = _policy.CalculateScore(suggestion, ClauseType.Select, "use");
            var scoreMixed = _policy.CalculateScore(suggestion, ClauseType.Select, "UsE");

            // Assert
            Assert.Equal(scoreUpper, scoreLower);
            Assert.Equal(scoreUpper, scoreMixed);
            Assert.True(scoreUpper > 0);
        }

        [Fact]
        public void PrefixMatch_EmptyToken_NoBonus()
        {
            // Arrange
            var suggestion = CreateSuggestion("UserId", SuggestionType.Column);

            // Act
            var scoreWithEmpty = _policy.CalculateScore(suggestion, ClauseType.Select, string.Empty);
            var scoreWithNull = _policy.CalculateScore(suggestion, ClauseType.Select, null);

            // Assert - should only have clause bonus, no prefix match bonus
            Assert.Equal(40, scoreWithEmpty);
            Assert.Equal(40, scoreWithNull);
        }

        // Helper method to create test suggestions
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
    }
}
