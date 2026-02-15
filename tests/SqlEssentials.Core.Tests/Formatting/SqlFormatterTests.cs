using System;
using SqlEssentials.Core.Formatting;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Formatting
{
    public sealed class SqlFormatterTests
    {
        [Fact]
        public void Format_ShouldUppercaseKeywords_AndPlaceJoinOnNewLine()
        {
            var logger = new RecordingLogger(LogLevel.Trace);
            var formatter = new SqlFormatter(logger);

            const string sql = "select c.customerid from sales.customers c join sales.orders o on c.customerid = o.customerid where c.customerid > 10";

            var result = formatter.Format(sql);

            Assert.True(result.Success);
            Assert.Contains("SELECT", result.FormattedText, StringComparison.Ordinal);
            Assert.Contains(Environment.NewLine + "FROM", result.FormattedText, StringComparison.Ordinal);
            Assert.Contains(Environment.NewLine + "JOIN", result.FormattedText, StringComparison.Ordinal);
            Assert.Contains(Environment.NewLine + "    ON", result.FormattedText, StringComparison.Ordinal);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message == "SQL formatting complete");
        }

        [Fact]
        public void FormatSelection_ShouldFormatOnlySelectedText()
        {
            var formatter = new SqlFormatter();
            const string sql = "select * from users;\nselect * from orders;";
            var start = sql.IndexOf("select * from orders", StringComparison.Ordinal);

            var result = formatter.FormatSelection(sql, start, "select * from orders;".Length);

            Assert.True(result.Success);
            Assert.StartsWith("SELECT", result.FormattedText, StringComparison.Ordinal);
            Assert.DoesNotContain("users", result.FormattedText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FormatSelection_WithNoSelection_ShouldFormatWholeDocument()
        {
            var formatter = new SqlFormatter();
            const string sql = "select * from users";

            var result = formatter.FormatSelection(sql, 0, 0);

            Assert.True(result.Success);
            Assert.Equal($"SELECT *{Environment.NewLine}FROM users", result.FormattedText);
        }
    }
}
