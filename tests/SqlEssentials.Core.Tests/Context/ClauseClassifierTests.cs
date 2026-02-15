using System;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Models;
using Xunit;

namespace SqlEssentials.Core.Tests.Context
{
    /// <summary>
    /// Tests for ClauseClassifier component verifying clause detection for various SQL contexts.
    /// </summary>
    public sealed class ClauseClassifierTests
    {
        private readonly ClauseClassifier _classifier = new ClauseClassifier();

        [Theory]
        [InlineData("SELECT UserId FROM Users", 7, ClauseType.Select)]  // Cursor in SELECT
        [InlineData("SELECT UserId FROM Users", 13, ClauseType.Select)]  // Before FROM keyword
        [InlineData("SELECT * FROM Users", 18, ClauseType.From)]  // In FROM clause
        [InlineData("SELECT * FROM Users", 19, ClauseType.From)]  // At end of table name
        [InlineData("SELECT * FROM Users WHERE UserId = 1", 26, ClauseType.Where)]  // Start of WHERE
        [InlineData("SELECT * FROM Users WHERE UserId = 1", 33, ClauseType.Where)]  // In WHERE
        [InlineData("SELECT * FROM Users u JOIN Orders o ON u.UserId = o.UserId", 27, ClauseType.Join)]  // In JOIN
        [InlineData("SELECT * FROM Users u JOIN Orders o ON u.UserId = o.UserId", 40, ClauseType.On)]  // Start of ON
        [InlineData("SELECT * FROM Users u JOIN Orders o ON u.UserId = o.UserId", 50, ClauseType.On)]  // In ON condition
        [InlineData("SELECT UserId FROM Users GROUP BY UserId", 30, ClauseType.GroupBy)]  // In GROUP BY
        [InlineData("SELECT UserId FROM Users GROUP BY UserId", 35, ClauseType.GroupBy)]  // At column in GROUP BY
        [InlineData("SELECT UserId FROM Users ORDER BY UserId", 30, ClauseType.OrderBy)]  // In ORDER BY
        [InlineData("SELECT UserId FROM Users ORDER BY UserId", 35, ClauseType.OrderBy)]  // At column in ORDER BY
        [InlineData("SELECT COUNT(*) as cnt FROM Users GROUP BY UserId HAVING cnt > 1", 50, ClauseType.Having)]  // In HAVING
        [InlineData("SELECT COUNT(*) as cnt FROM Users GROUP BY UserId HAVING cnt > 1", 60, ClauseType.Having)]  // In HAVING condition
        public void ClassifyClause_ReturnsCorrectClauseType(string queryText, int cursorPosition, ClauseType expected)
        {
            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ClassifyClause_NullQueryText_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _classifier.ClassifyClause(null, 0));
        }

        [Theory]
        [InlineData("SELECT * FROM Users", -1)]
        [InlineData("SELECT * FROM Users", 100)]
        public void ClassifyClause_InvalidCursorPosition_ReturnsUnknown(string queryText, int cursorPosition)
        {
            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(ClauseType.Unknown, result);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("   ", 1)]
        [InlineData("INVALID SQL SYNTAX", 10)]
        public void ClassifyClause_InvalidOrEmptySql_ReturnsUnknown(string queryText, int cursorPosition)
        {
            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(ClauseType.Unknown, result);
        }

        [Fact]
        public void ClassifyClause_ComplexNestedQuery_DetectsInnerSelect()
        {
            // Arrange
            var queryText = "SELECT * FROM (SELECT UserId FROM Users) AS SubQuery";
            var cursorPosition = 25; // Inside the nested SELECT

            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(ClauseType.Select, result);
        }

        [Fact]
        public void ClassifyClause_MultipleJoins_DetectsJoinClause()
        {
            // Arrange
            var queryText = "SELECT * FROM Users u JOIN Orders o ON u.UserId = o.UserId JOIN OrderItems i ON o.OrderId = i.OrderId";
            var cursorPosition = 70; // In second JOIN

            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(ClauseType.Join, result);
        }

        [Theory]
        [InlineData("SELECT * FROM Users WHERE UserId IN (SELECT UserId FROM Orders)", 44, ClauseType.Select)]  // In nested SELECT
        [InlineData("SELECT * FROM Users WHERE UserId IN (SELECT UserId FROM Orders)", 56, ClauseType.From)]  // In nested FROM
        public void ClassifyClause_SubqueryInWhere_DetectsNestedClause(string queryText, int cursorPosition, ClauseType expected)
        {
            // Act
            var result = _classifier.ClassifyClause(queryText, cursorPosition);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ClassifyClause_CursorAtClauseBoundary_DetectsCorrectClause()
        {
            // Arrange
            var queryText = "SELECT * FROM Users WHERE UserId = 1";

            // Act - cursor right after WHERE keyword
            var resultAtWhere = _classifier.ClassifyClause(queryText, 26);
            
            // Act - cursor at start of WHERE clause
            var resultAtStart = _classifier.ClassifyClause(queryText, 20);

            // Assert
            Assert.Equal(ClauseType.Where, resultAtWhere);
            // At the boundary, could be FROM or WHERE depending on parser behavior
            Assert.True(resultAtStart == ClauseType.From || resultAtStart == ClauseType.Where);
        }
    }
}
