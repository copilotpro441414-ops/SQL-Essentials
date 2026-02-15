using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Snippets;

namespace SqlEssentials.Core.Completion
{
    public sealed class CompletionEngine : ICompletionEngine
    {
        private readonly IContextAnalyzer _contextAnalyzer;
        private readonly ISchemaCache _schemaCache;
        private readonly ILogger _logger;
        private readonly SuggestionScoringPolicy _scoringPolicy;
        private readonly JoinPredicateGenerator _joinPredicateGenerator;
        private readonly KeywordProvider _keywordProvider;
        private readonly ISnippetManager _snippetManager;

        public CompletionEngine(
            IContextAnalyzer contextAnalyzer,
            ISchemaCache schemaCache,
            ILogger logger = null,
            SuggestionScoringPolicy scoringPolicy = null,
            JoinPredicateGenerator joinPredicateGenerator = null,
            KeywordProvider keywordProvider = null,
            ISnippetManager snippetManager = null)
        {
            _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
            _schemaCache = schemaCache ?? throw new ArgumentNullException(nameof(schemaCache));
            _logger = logger ?? NullLogger.Instance;
            _scoringPolicy = scoringPolicy ?? new SuggestionScoringPolicy();
            _joinPredicateGenerator = joinPredicateGenerator ?? new JoinPredicateGenerator(_logger);
            _keywordProvider = keywordProvider ?? new KeywordProvider();
            _snippetManager = snippetManager ?? new SnippetManager(_logger);
        }

        public async Task<ICompletionResult> GetCompletionsAsync(
            string queryText,
            int cursorPosition,
            string connectionKey,
            TriggerReason trigger,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            using (var scope = _logger.BeginScope("CompletionEngine", "GetCompletions", correlationId: correlationId))
            {
                try
                {
                    _logger.Log(LogLevel.Debug, "CompletionEngine", "GetCompletionsAsync started", properties: new Dictionary<string, object>
                    {
                        { "cursor_position", cursorPosition },
                        { "connection_key", connectionKey ?? string.Empty },
                        { "trigger", trigger.ToString() }
                    }, correlationId: correlationId);

                    var context = _contextAnalyzer.Analyze(queryText, cursorPosition, correlationId);
                    _logger.Log(LogLevel.Debug, "CompletionEngine", "Context analysis complete", properties: new Dictionary<string, object>
                    {
                        { "clause", context.CurrentClause.ToString() },
                        { "alias_count", context.Aliases?.Count ?? 0 },
                        { "qualifier", context.QualifierPrefix ?? string.Empty }
                    }, correlationId: correlationId);

                    var cache = await _schemaCache.GetOrLoadAsync(connectionKey, correlationId, cancellationToken).ConfigureAwait(false);
                    _logger.Log(LogLevel.Debug, "CompletionEngine", "Cache lookup complete", properties: new Dictionary<string, object>
                    {
                        { "cache_status", cache?.Status.ToString() ?? string.Empty },
                        { "table_count", cache?.Tables?.Count ?? 0 },
                        { "view_count", cache?.Views?.Count ?? 0 }
                    }, correlationId: correlationId);

                    var suggestions = new List<ISuggestion>();
                    var typedToken = GetTypedToken(context.PartialInput);

                    if (cache != null)
                    {
                        if (context.CurrentClause == ClauseType.On && context.JoinContext != null)
                        {
                            var joinSuggestions = _joinPredicateGenerator.Generate(context.JoinContext, cache, correlationId);
                            suggestions.AddRange(joinSuggestions);
                        }

                        if (!string.IsNullOrWhiteSpace(context.QualifierPrefix))
                        {
                            var qualifier = context.QualifierPrefix;
                            var table = ResolveQualifiedTable(context, cache, qualifier);
                            if (table != null)
                            {
                                var columnSuggestions = CreateColumnSuggestions(table).ToList();
                                suggestions.AddRange(columnSuggestions);
                                _logger.Log(LogLevel.Trace, "CompletionEngine", "Column-only filter applied", properties: new Dictionary<string, object>
                                {
                                    { "alias", qualifier },
                                    { "resolved_table", table.FullyQualifiedName ?? table.ObjectName },
                                    { "column_count", columnSuggestions.Count }
                                }, correlationId: correlationId);
                            }
                            else
                            {
                                _logger.Log(LogLevel.Trace, "CompletionEngine", "Column-only filter alias unresolved", properties: new Dictionary<string, object>
                                {
                                    { "alias", qualifier }
                                }, correlationId: correlationId);
                            }
                        }
                        else
                        {
                            suggestions.AddRange(CreateTableSuggestions(cache.Tables, SuggestionType.Table));
                            suggestions.AddRange(CreateTableSuggestions(cache.Views, SuggestionType.View));
                            suggestions.AddRange(CreateColumnSuggestions(cache.Tables));
                            suggestions.AddRange(CreateColumnSuggestions(cache.Views));

                            if (context.CurrentClause != ClauseType.On && !string.IsNullOrWhiteSpace(typedToken) && typedToken.Length >= 3)
                            {
                                suggestions.AddRange(_keywordProvider.GetSuggestions(typedToken));
                                suggestions.AddRange(CreateSnippetSuggestions(_snippetManager.FindByPrefix(typedToken)));
                            }
                        }
                    }

                    var rankedSuggestions = RankSuggestions(suggestions, context, typedToken, correlationId);
                    var applicableSpan = new TextSpan(
                        Math.Max(0, context.CursorPosition - typedToken.Length),
                        typedToken.Length);

                    _logger.Log(LogLevel.Info, "CompletionEngine", $"Found {rankedSuggestions.Count} suggestions", 
                        properties: new Dictionary<string, object> { { "clause", context.CurrentClause.ToString() } },
                        correlationId: correlationId);

                    return new CompletionResult(rankedSuggestions, applicableSpan, shouldFilter: true, preselectedIndex: -1);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "CompletionEngine", "GetCompletionsAsync failed", ex, correlationId: correlationId);
                    throw;
                }
            }
        }

        private static ITableDefinition ResolveQualifiedTable(IAutocompleteContext context, IDatabaseCache cache, string qualifier)
        {
            if (context.Aliases != null && context.Aliases.TryGetValue(qualifier, out var binding))
            {
                var fullyQualified = binding.FullyQualifiedName;
                var byFullName = cache.FindTable(fullyQualified);
                if (byFullName != null)
                {
                    return byFullName;
                }

                var byName = cache.FindTable(binding.TableName);
                if (byName != null)
                {
                    return byName;
                }
            }

            return cache.FindTable(qualifier);
        }

        private static string GetTypedToken(string partialInput)
        {
            if (string.IsNullOrWhiteSpace(partialInput))
            {
                return string.Empty;
            }

            var lastDot = partialInput.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < partialInput.Length - 1)
            {
                return partialInput.Substring(lastDot + 1);
            }

            return partialInput;
        }

        private static IEnumerable<ISuggestion> CreateTableSuggestions(IEnumerable<ITableDefinition> tables, SuggestionType type)
        {
            foreach (var table in tables)
            {
                yield return new Suggestion(
                    table.ObjectName,
                    table.ObjectName,
                    type,
                    table.FullyQualifiedName,
                    string.Empty,
                    relevanceScore: 0,
                    filterText: table.ObjectName,
                    sortText: table.ObjectName);
            }
        }

        private static IEnumerable<ISuggestion> CreateColumnSuggestions(IEnumerable<ITableDefinition> tables)
        {
            foreach (var table in tables)
            {
                foreach (var column in RankColumns(table.Columns))
                {
                    yield return new Suggestion(
                        column.Name,
                        column.Name,
                        SuggestionType.Column,
                        table.FullyQualifiedName,
                        column.DataType,
                        relevanceScore: column.IsPrimaryKey ? 20 : 0,
                        filterText: column.Name,
                        sortText: column.Name);
                }
            }
        }

        private static IEnumerable<ISuggestion> CreateColumnSuggestions(ITableDefinition table)
        {
            foreach (var column in RankColumns(table.Columns))
            {
                yield return new Suggestion(
                    column.Name,
                    column.Name,
                    SuggestionType.Column,
                    table.FullyQualifiedName,
                    column.DataType,
                    relevanceScore: column.IsPrimaryKey ? 20 : 0,
                    filterText: column.Name,
                    sortText: column.Name);
            }
        }

        private static IEnumerable<IColumnDefinition> RankColumns(IEnumerable<IColumnDefinition> columns)
        {
            return columns
                .OrderByDescending(column => column.IsPrimaryKey)
                .ThenBy(column => column.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<ISuggestion> CreateSnippetSuggestions(IEnumerable<ISnippet> snippets)
        {
            if (snippets == null)
            {
                yield break;
            }

            foreach (var snippet in snippets)
            {
                yield return new Suggestion(
                    snippet.Shortcut,
                    snippet.ExpandedBody,
                    SuggestionType.Snippet,
                    string.IsNullOrWhiteSpace(snippet.Description) ? snippet.Name : snippet.Description,
                    string.Empty,
                    relevanceScore: 0,
                    filterText: snippet.Shortcut,
                    sortText: snippet.Shortcut);
            }
        }

        private IReadOnlyList<ISuggestion> RankSuggestions(
            IEnumerable<ISuggestion> suggestions,
            IAutocompleteContext context,
            string typedToken,
            string correlationId)
        {
            var clause = context.CurrentClause;
            var sourceSuggestions = suggestions.ToList();

            foreach (var suggestionType in sourceSuggestions.Select(s => s.Type).Distinct())
            {
                _logger.Log(LogLevel.Debug, "CompletionEngine", "Clause bonus applied", properties: new Dictionary<string, object>
                {
                    { "clause", clause.ToString() },
                    { "suggestion_type", suggestionType.ToString() },
                    { "clause_bonus", _scoringPolicy.GetClauseBonus(suggestionType, clause) }
                }, correlationId: correlationId);
            }

            return sourceSuggestions
                .OrderByDescending(suggestion => _scoringPolicy.CalculateScore(suggestion, clause, typedToken))
                .ThenBy(suggestion => suggestion.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetSuggestionScore(ISuggestion suggestion, ClauseType clause, string typedToken)
        {
            // Deprecated: Use SuggestionScoringPolicy.CalculateScore instead
            // Kept temporarily for compatibility
            if (suggestion == null)
            {
                throw new ArgumentNullException(nameof(suggestion));
            }
            var policy = new SuggestionScoringPolicy();
            return policy.CalculateScore(suggestion, clause, typedToken);
        }

        private static int GetClauseBonus(SuggestionType type, ClauseType clause)
        {
            // Deprecated: Use SuggestionScoringPolicy.GetClauseBonus instead
            // Kept temporarily for compatibility
            var policy = new SuggestionScoringPolicy();
            return policy.GetClauseBonus(type, clause);
        }

        private sealed class CompletionResult : ICompletionResult
        {
            public CompletionResult(IReadOnlyList<ISuggestion> suggestions, TextSpan applicableSpan, bool shouldFilter, int preselectedIndex)
            {
                Suggestions = suggestions ?? Array.Empty<ISuggestion>();
                ApplicableSpan = applicableSpan;
                ShouldFilter = shouldFilter;
                PreselectedIndex = preselectedIndex;
            }

            public IReadOnlyList<ISuggestion> Suggestions { get; }
            public TextSpan ApplicableSpan { get; }
            public bool ShouldFilter { get; }
            public int PreselectedIndex { get; }
        }
    }
}
