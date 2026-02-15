using System.Collections.Generic;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;
using Xunit;

namespace SqlEssentials.Core.Tests.Context
{
    public sealed class ContextAnalyzerEdgeCaseTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("SELECT 1", -1)]
        [InlineData("SELECT 1", 99)]
        public void Analyze_InvalidCursor_DoesNotThrow(string sql, int cursor)
        {
            var analyzer = new ContextAnalyzer(NullLogger.Instance);
            var context = analyzer.Analyze(sql, cursor);

            Assert.NotNull(context);
            Assert.NotNull(context.PartialInput);
            Assert.NotNull(context.QualifierPrefix);
        }

        [Fact]
        public void Analyze_BracketedQualifier_DetectsPrefix()
        {
            var analyzer = new ContextAnalyzer(NullLogger.Instance);
            var context = analyzer.Analyze("SELECT [u]. FROM dbo.Users u", 11);

            Assert.Equal("[u]", context.QualifierPrefix);
        }

        [Fact]
        public void IsInNonCodeContext_TrueForCommentAndString()
        {
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var inComment = analyzer.IsInNonCodeContext("SELECT 1 -- comment", 13);
            var inString = analyzer.IsInNonCodeContext("SELECT 'abc'", 9);

            Assert.True(inComment);
            Assert.True(inString);
        }

        [Fact]
        public void Analyze_ExtractsAliasesFromJoinQuery()
        {
            var analyzer = new ContextAnalyzer(NullLogger.Instance);
            var context = analyzer.Analyze("SELECT * FROM dbo.Users u JOIN dbo.Orders o ON u.UserId = o.UserId", 25);

            Assert.True(context.Aliases.ContainsKey("u"));
            Assert.True(context.Aliases.ContainsKey("o"));
        }
    }
}
