using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;
using Xunit;

namespace SqlEssentials.Core.Tests.Context
{
    public sealed class ContextAnalyzerJoinContextTests
    {
        [Fact]
        public void Analyze_OnClauseWithAliases_ExtractsJoinContext()
        {
            var sql = "SELECT * FROM dbo.Users u JOIN dbo.Orders o ON u.UserId = o.UserId";
            var cursor = sql.IndexOf("ON", System.StringComparison.OrdinalIgnoreCase) + 3;
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var context = analyzer.Analyze(sql, cursor);

            Assert.Equal(ClauseType.On, context.CurrentClause);
            Assert.NotNull(context.JoinContext);
            Assert.Equal("u", context.JoinContext.LeftTableOrAlias);
            Assert.Equal("o", context.JoinContext.RightTableOrAlias);
            Assert.Equal("Users", context.JoinContext.ResolvedLeftTable);
            Assert.Equal("Orders", context.JoinContext.ResolvedRightTable);
        }

        [Fact]
        public void Analyze_OnClauseWithoutAliases_ExtractsTableNames()
        {
            var sql = "SELECT * FROM dbo.Users JOIN dbo.Orders ON dbo.Users.UserId = dbo.Orders.UserId";
            var cursor = sql.IndexOf("ON", System.StringComparison.OrdinalIgnoreCase) + 3;
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var context = analyzer.Analyze(sql, cursor);

            Assert.Equal(ClauseType.On, context.CurrentClause);
            Assert.NotNull(context.JoinContext);
            Assert.Equal("Users", context.JoinContext.LeftTableOrAlias);
            Assert.Equal("Orders", context.JoinContext.RightTableOrAlias);
            Assert.Equal("Users", context.JoinContext.ResolvedLeftTable);
            Assert.Equal("Orders", context.JoinContext.ResolvedRightTable);
        }

        [Fact]
        public void Analyze_NotInOnClause_JoinContextIsNull()
        {
            var sql = "SELECT * FROM dbo.Users u JOIN dbo.Orders o";
            var cursor = sql.IndexOf("JOIN", System.StringComparison.OrdinalIgnoreCase) + 2;
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var context = analyzer.Analyze(sql, cursor);

            Assert.NotEqual(ClauseType.On, context.CurrentClause);
            Assert.Null(context.JoinContext);
        }

        [Fact]
        public void Analyze_OnClauseMalformedJoin_JoinContextIsNull()
        {
            var sql = "SELECT * FROM dbo.Users u ON u.";
            var cursor = sql.Length;
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var context = analyzer.Analyze(sql, cursor);

            if (context.CurrentClause == ClauseType.On)
            {
                Assert.Null(context.JoinContext);
            }
            else
            {
                Assert.Null(context.JoinContext);
            }
        }

        [Fact]
        public void Analyze_OnClauseWithPartialPredicate_StillExtractsJoinContext()
        {
            var sql = "SELECT * FROM Users u JOIN Orders o ON ";
            var cursor = sql.Length;
            var analyzer = new ContextAnalyzer(NullLogger.Instance);

            var context = analyzer.Analyze(sql, cursor);

            Assert.NotNull(context);
            if (context.CurrentClause == ClauseType.On)
            {
                Assert.NotNull(context.JoinContext);
                Assert.Equal("u", context.JoinContext.LeftTableOrAlias);
                Assert.Equal("o", context.JoinContext.RightTableOrAlias);
            }
            else
            {
                Assert.Equal(ClauseType.Unknown, context.CurrentClause);
                Assert.Null(context.JoinContext);
            }
        }
    }
}