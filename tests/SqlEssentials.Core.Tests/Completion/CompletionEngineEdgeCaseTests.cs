using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Completion
{
    public sealed class CompletionEngineEdgeCaseTests
    {
        [Fact]
        public void Constructor_ThrowsForNullDependencies()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var analyzer = new StubContextAnalyzer(CreateContext(partialInput: "Us", qualifierPrefix: null));

            Assert.Throws<ArgumentNullException>(() => new CompletionEngine(null, cache));
            Assert.Throws<ArgumentNullException>(() => new CompletionEngine(analyzer, null));
        }

        [Fact]
        public async Task GetCompletionsAsync_ReturnsEmpty_WhenCacheIsNull()
        {
            var analyzer = new StubContextAnalyzer(CreateContext(partialInput: "Us", qualifierPrefix: null));
            var engine = new CompletionEngine(analyzer, new NullSchemaCache(), NullLogger.Instance);

            var result = await engine.GetCompletionsAsync("SELECT Us", 8, "conn-A", TriggerReason.Typing);

            Assert.NotNull(result);
            Assert.Empty(result.Suggestions);
            Assert.Equal(2, result.ApplicableSpan.Length);
        }

        [Fact]
        public async Task QualifiedAlias_FallsBackToTableName_WhenFullNameNotFound()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var aliases = new Dictionary<string, IAliasBinding>(StringComparer.OrdinalIgnoreCase)
            {
                ["u"] = new AliasBinding("u", "sales", "Users", 0)
            };

            var context = new AutocompleteContext(
                "SELECT u.",
                9,
                ClauseType.Select,
                "u.",
                "u",
                aliases,
                new string[0],
                TriggerReason.DotAfterIdentifier);

            var engine = new CompletionEngine(new StubContextAnalyzer(context), cache, NullLogger.Instance);
            var result = await engine.GetCompletionsAsync("SELECT u.", 9, "conn-A", TriggerReason.DotAfterIdentifier);

            Assert.Contains(result.Suggestions, s => s.DisplayText == "UserId");
            Assert.All(result.Suggestions, s => Assert.Equal(SuggestionType.Column, s.Type));
        }

        [Fact]
        public async Task GetCompletionsAsync_PropagatesAnalyzerException()
        {
            var engine = new CompletionEngine(new ThrowingContextAnalyzer(), new NullSchemaCache(), new RecordingLogger(LogLevel.Trace));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                engine.GetCompletionsAsync("SELECT", 0, "conn-A", TriggerReason.Typing));
        }

        [Fact]
        public void DeprecatedGetSuggestionScore_ThrowsForNullSuggestion()
        {
            var method = typeof(CompletionEngine).GetMethod("GetSuggestionScore", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { null, ClauseType.Select, "Us" }));
            Assert.IsType<ArgumentNullException>(exception.InnerException);
        }

        [Fact]
        public void DeprecatedGetSuggestionScore_CalculatesExpectedScore()
        {
            var method = typeof(CompletionEngine).GetMethod("GetSuggestionScore", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var suggestion = new Suggestion("Users", "Users", SuggestionType.Table, "dbo.Users", string.Empty, 0, "Users", "Users");
            var score = (int)method.Invoke(null, new object[] { suggestion, ClauseType.From, "Us" });

            Assert.Equal(90, score);
        }

        [Fact]
        public void DeprecatedGetClauseBonus_ReturnsExpectedValue()
        {
            var method = typeof(CompletionEngine).GetMethod("GetClauseBonus", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var bonus = (int)method.Invoke(null, new object[] { SuggestionType.View, ClauseType.Join });
            Assert.Equal(40, bonus);
        }

        private static IAutocompleteContext CreateContext(string partialInput, string qualifierPrefix)
        {
            return new AutocompleteContext(
                "SELECT",
                6,
                ClauseType.Select,
                partialInput,
                qualifierPrefix,
                new Dictionary<string, IAliasBinding>(StringComparer.OrdinalIgnoreCase),
                new string[0],
                TriggerReason.Typing);
        }

        private sealed class StubContextAnalyzer : IContextAnalyzer
        {
            private readonly IAutocompleteContext _context;

            public StubContextAnalyzer(IAutocompleteContext context)
            {
                _context = context;
            }

            public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null) => _context;

            public IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText) => _context.Aliases;

            public bool IsInNonCodeContext(string queryText, int cursorPosition) => false;
        }

        private sealed class ThrowingContextAnalyzer : IContextAnalyzer
        {
            public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null)
            {
                throw new InvalidOperationException("Analyzer failed");
            }

            public IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText)
            {
                return new Dictionary<string, IAliasBinding>();
            }

            public bool IsInNonCodeContext(string queryText, int cursorPosition) => false;
        }

        private sealed class NullSchemaCache : ISchemaCache
        {
            public CacheStatus GetStatus(string connectionKey) => CacheStatus.Empty;

            public Task<IDatabaseCache> GetOrLoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IDatabaseCache>(null);
            }

            public Task RefreshAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Invalidate(string connectionKey)
            {
            }

            public void InvalidateAll()
            {
            }

            public TimeSpan TimeToLive { get; set; }

            public event EventHandler<CacheStatusChangedEventArgs> StatusChanged
            {
                add { }
                remove { }
            }
        }
    }
}
