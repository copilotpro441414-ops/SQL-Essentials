using System.Collections.Generic;
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
    public sealed class CompletionEngineJoinPredicateTests
    {
        [Fact]
        public async Task GetCompletionsAsync_OnClauseWithJoinContext_IncludesJoinPredicateSuggestion()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var context = new AutocompleteContext(
                "SELECT * FROM Users u JOIN Orders o ON ",
                38,
                ClauseType.On,
                string.Empty,
                string.Empty,
                new Dictionary<string, IAliasBinding>(),
                new string[0],
                TriggerReason.Typing,
                new JoinContext("u", "o", "Users", "Orders"));

            var engine = new CompletionEngine(new StubContextAnalyzer(context), cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync(context.QueryText, context.CursorPosition, "conn-A", TriggerReason.Typing);

            Assert.Contains(result.Suggestions, s => s.Type == SuggestionType.JoinPredicate);
            Assert.Contains(result.Suggestions, s => s.InsertionText == "u.UserId = o.UserId");
        }

        [Fact]
        public async Task GetCompletionsAsync_OnClauseWithoutJoinContext_DoesNotIncludeJoinPredicateSuggestion()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var context = new AutocompleteContext(
                "SELECT * FROM Users u JOIN Orders o ON ",
                38,
                ClauseType.On,
                string.Empty,
                string.Empty,
                new Dictionary<string, IAliasBinding>(),
                new string[0],
                TriggerReason.Typing,
                joinContext: null);

            var engine = new CompletionEngine(new StubContextAnalyzer(context), cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync(context.QueryText, context.CursorPosition, "conn-A", TriggerReason.Typing);

            Assert.DoesNotContain(result.Suggestions, s => s.Type == SuggestionType.JoinPredicate);
            Assert.Contains(result.Suggestions, s => s.Type == SuggestionType.Table || s.Type == SuggestionType.Column);
        }

        private sealed class StubContextAnalyzer : IContextAnalyzer
        {
            private readonly IAutocompleteContext _context;

            public StubContextAnalyzer(IAutocompleteContext context)
            {
                _context = context;
            }

            public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null)
            {
                return _context;
            }

            public IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText)
            {
                return _context.Aliases;
            }

            public bool IsInNonCodeContext(string queryText, int cursorPosition)
            {
                return false;
            }
        }
    }
}
