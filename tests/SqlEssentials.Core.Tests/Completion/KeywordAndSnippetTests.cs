using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Snippets;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Completion
{
    public sealed class KeywordAndSnippetTests
    {
        [Fact]
        public void KeywordProvider_ReturnsPrefixMatches()
        {
            var provider = new KeywordProvider();

            var suggestions = provider.GetSuggestions("SEL");

            Assert.Contains(suggestions, s => s.DisplayText == "SELECT");
            Assert.All(suggestions, s => Assert.StartsWith("SEL", s.DisplayText, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CompletionEngine_ReturnsKeywordAndSnippetSuggestions_ForTypedPrefix()
        {
            var cache = new FakeSchemaCache(TestMetadataBuilder.CreateSimpleCache("conn-A"));
            var context = new AutocompleteContext(
                "sel",
                3,
                ClauseType.Select,
                "sel",
                string.Empty,
                new Dictionary<string, IAliasBinding>(),
                new string[0],
                TriggerReason.Typing,
                joinContext: null);

            var logger = new RecordingLogger(LogLevel.Trace);
            var engine = new CompletionEngine(
                new StubContextAnalyzer(context),
                cache,
                logger,
                keywordProvider: new KeywordProvider(),
                snippetManager: new SnippetManager(logger));

            var result = await engine.GetCompletionsAsync(context.QueryText, context.CursorPosition, "conn-A", TriggerReason.Typing);

            Assert.Contains(result.Suggestions, s => s.Type == SuggestionType.Keyword && s.DisplayText == "SELECT");
            Assert.Contains(result.Suggestions, s => s.Type == SuggestionType.Snippet && s.DisplayText == "sel");
        }

        [Fact]
        public void SnippetExpansionSession_MovesPlaceholdersInOrder()
        {
            var snippet = new Snippet(
                "selj",
                "Select Join",
                "Sample",
                "Query",
                new[] { "SELECT ${1:*} FROM ${2:Table}" },
                new Dictionary<int, IPlaceholderDefinition>
                {
                    { 1, new PlaceholderDefinition(1, "*") },
                    { 2, new PlaceholderDefinition(2, "Table") }
                },
                isBuiltIn: false);

            var session = new SnippetExpansionSession(
                snippet,
                0,
                new[]
                {
                    new PlaceholderSpan(1, 7, 1, "*"),
                    new PlaceholderSpan(2, 14, 5, "Table")
                },
                isComplete: false);

            var moved = session.MoveNext();
            Assert.False(moved.IsComplete);
            Assert.Equal(1, moved.CurrentPlaceholderIndex);

            var done = moved.MoveNext();
            Assert.True(done.IsComplete);
        }

        [Fact]
        public void SnippetManager_Throws_WhenSnippetBodyMissing()
        {
            var manager = new SnippetManager(NullLogger.Instance);
            var emptyBodySnippet = new Snippet(
                "empty",
                "Empty",
                "No body",
                "Test",
                new[] { "body" },
                new Dictionary<int, IPlaceholderDefinition>(),
                isBuiltIn: false);

            var invalid = new InvalidSnippet(emptyBodySnippet.Shortcut);
            Assert.Throws<ArgumentException>(() => manager.AddCustomSnippet(invalid));
        }

        [Fact]
        public void SnippetManager_Throws_OnInvalidPlaceholderInJson()
        {
            var manager = new SnippetManager(NullLogger.Instance);
            var invalidJson = "{\"snippets\":[{\"shortcut\":\"x\",\"name\":\"X\",\"description\":\"\",\"category\":\"\",\"body\":[\"SELECT 1\"],\"placeholders\":{\"abc\":{\"default\":\"x\"}}}]}";

            Assert.Throws<FormatException>(() => manager.ImportFromJson(invalidJson));
        }

        [Fact]
        public void SnippetManager_Throws_OnAmbiguousShortcutCollision()
        {
            var manager = new SnippetManager(NullLogger.Instance);
            var duplicateBuiltIn = new Snippet(
                "sel",
                "Duplicate",
                "Conflict",
                "Test",
                new[] { "SELECT 1" },
                new Dictionary<int, IPlaceholderDefinition>(),
                isBuiltIn: false);

            Assert.Throws<InvalidOperationException>(() => manager.AddCustomSnippet(duplicateBuiltIn));
        }

        [Fact]
        public void SnippetManager_LogsLoadAndMutation()
        {
            var logger = new RecordingLogger(LogLevel.Debug);
            var manager = new SnippetManager(logger);
            var custom = new Snippet(
                "myq",
                "My Query",
                "Custom",
                "Test",
                new[] { "SELECT 1" },
                new Dictionary<int, IPlaceholderDefinition>(),
                isBuiltIn: false);

            manager.AddCustomSnippet(custom);
            manager.UpdateCustomSnippet("myq", new Snippet("myq2", "My Query 2", "Updated", "Test", new[] { "SELECT 2" }, new Dictionary<int, IPlaceholderDefinition>(), false));
            manager.RemoveCustomSnippet("myq2");

            Assert.Contains(logger.Entries, e => e.Component == "SnippetManager" && e.Message.Contains("Built-in snippets loaded"));
            Assert.Contains(logger.Entries, e => e.Component == "SnippetManager" && e.Message.Contains("Custom snippet added"));
            Assert.Contains(logger.Entries, e => e.Component == "SnippetManager" && e.Message.Contains("Custom snippet updated"));
            Assert.Contains(logger.Entries, e => e.Component == "SnippetManager" && e.Message.Contains("Custom snippet removed"));
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

        private sealed class InvalidSnippet : ISnippet
        {
            public InvalidSnippet(string shortcut)
            {
                Shortcut = shortcut;
            }

            public string Shortcut { get; }
            public string Name => "Invalid";
            public string Description => string.Empty;
            public string Category => string.Empty;
            public IReadOnlyList<string> BodyLines => Array.Empty<string>();
            public IReadOnlyDictionary<int, IPlaceholderDefinition> Placeholders => new Dictionary<int, IPlaceholderDefinition>();
            public bool IsBuiltIn => false;
            public string ExpandedBody => string.Empty;
        }
    }
}