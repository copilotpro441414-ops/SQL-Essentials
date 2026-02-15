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
    public sealed class CompletionAliasQualifiedTests
    {
        [Fact]
        public async Task AliasQualified_ReturnsOnlyAliasTableColumns()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var analyzer = new StubContextAnalyzer(CreateContext("u", "Users", true));
            var engine = new CompletionEngine(analyzer, cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync("SELECT u.", 9, "conn-A", TriggerReason.DotAfterIdentifier);

            Assert.NotEmpty(result.Suggestions);
            Assert.All(result.Suggestions, suggestion => Assert.Equal(SuggestionType.Column, suggestion.Type));
            Assert.Contains(result.Suggestions, suggestion => suggestion.DisplayText == "UserId");
            Assert.Contains(result.Suggestions, suggestion => suggestion.DisplayText == "Username");
            Assert.DoesNotContain(result.Suggestions, suggestion => suggestion.DisplayText == "OrderId");
        }

        [Fact]
        public async Task UnresolvedAlias_ReturnsNoSuggestions()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var analyzer = new StubContextAnalyzer(CreateContext("x", "Users", false));
            var engine = new CompletionEngine(analyzer, cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync("SELECT x.", 9, "conn-A", TriggerReason.DotAfterIdentifier);

            Assert.Empty(result.Suggestions);
        }

        [Fact]
        public async Task DirectTableQualifier_ResolvesTableByName()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var analyzer = new StubContextAnalyzer(new AutocompleteContext(
                "SELECT Users.",
                13,
                ClauseType.Select,
                "Users.",
                "Users",
                new System.Collections.Generic.Dictionary<string, IAliasBinding>(),
                new string[0],
                TriggerReason.DotAfterIdentifier));
            var engine = new CompletionEngine(analyzer, cache, NullLogger.Instance);

            var result = await engine.GetCompletionsAsync("SELECT Users.", 13, "conn-A", TriggerReason.DotAfterIdentifier);

            Assert.NotEmpty(result.Suggestions);
            Assert.All(result.Suggestions, suggestion => Assert.Equal(SuggestionType.Column, suggestion.Type));
            Assert.Contains(result.Suggestions, suggestion => suggestion.DisplayText == "UserId");
        }

        private static IAutocompleteContext CreateContext(string qualifier, string tableName, bool includeAlias)
        {
            var aliases = new System.Collections.Generic.Dictionary<string, IAliasBinding>(System.StringComparer.OrdinalIgnoreCase);
            if (includeAlias)
            {
                aliases[qualifier] = new AliasBinding(qualifier, "dbo", tableName, 0);
            }

            return new AutocompleteContext(
                "SELECT " + qualifier + ".",
                9,
                ClauseType.Select,
                qualifier + ".",
                qualifier,
                aliases,
                new string[0],
                TriggerReason.DotAfterIdentifier);
        }

        private sealed class StubContextAnalyzer : IContextAnalyzer
        {
            private readonly IAutocompleteContext _context;

            public StubContextAnalyzer(IAutocompleteContext context)
            {
                _context = context;
            }

            public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null) => _context;

            public System.Collections.Generic.IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText)
                => _context.Aliases;

            public bool IsInNonCodeContext(string queryText, int cursorPosition) => false;
        }
    }
}
