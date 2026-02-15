using System.Linq;
using System.Threading.Tasks;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Completion
{
    public sealed class CompletionClausePriorityTests
    {
        [Theory]
        [InlineData("SELECT * FROM Users", 14, SuggestionType.Table)]
        [InlineData("SELECT UserId FROM Users", 8, SuggestionType.Column)]
        [InlineData("SELECT * FROM Users WHERE ", 26, SuggestionType.Column)]
        [InlineData("SELECT * FROM Users u JOIN Orders o ON ", 37, SuggestionType.Column)]
        public async Task ClausePriority_ReturnsExpectedTopType(string sql, int cursor, SuggestionType expectedTopType)
        {
            var cache = new FakeSchemaCache(
                TestMetadataBuilder.CreateSimpleCache("conn-A"));

            var analyzer = new ContextAnalyzer(NullLogger.Instance);
            var engine = new CompletionEngine(analyzer, cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync(sql, cursor, "conn-A", TriggerReason.Typing);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Suggestions);
            Assert.Equal(expectedTopType, result.Suggestions.First().Type);
        }
    }
}
