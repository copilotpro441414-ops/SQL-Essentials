using SqlEssentials.Core.Formatting;
using Xunit;

namespace SqlEssentials.Core.Tests.Formatting
{
    public sealed class SqlFormatterEdgeCaseTests
    {
        [Fact]
        public void Format_MalformedSql_ShouldReturnFailureAndOriginalText()
        {
            var formatter = new SqlFormatter();
            const string sql = "SELECT FROM";

            var result = formatter.Format(sql);

            Assert.False(result.Success);
            Assert.Equal(sql, result.FormattedText);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void FormatSelection_InvalidRange_ShouldReturnFailure()
        {
            var formatter = new SqlFormatter();
            const string sql = "SELECT * FROM Users";

            var result = formatter.FormatSelection(sql, -1, 5);

            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Format_MixedWhitespace_ShouldNormalizeSpacing()
        {
            var formatter = new SqlFormatter();
            const string sql = "select\t*   from    Users\r\nwhere   UserId=1";

            var result = formatter.Format(sql);

            Assert.True(result.Success);
            Assert.DoesNotContain("\t", result.FormattedText);
            Assert.Contains("SELECT *", result.FormattedText);
            Assert.Contains("FROM Users", result.FormattedText);
        }
    }
}
