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

namespace SqlEssentials.Core.Completion
{
    public sealed class CompletionEngine : ICompletionEngine
    {
        private readonly IContextAnalyzer _contextAnalyzer;
        private readonly ISchemaCache _schemaCache;
        private readonly ILogger _logger;

        public CompletionEngine(IContextAnalyzer contextAnalyzer, ISchemaCache schemaCache, ILogger logger = null)
        {
            _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
            _schemaCache = schemaCache ?? throw new ArgumentNullException(nameof(schemaCache));
            _logger = logger ?? NullLogger.Instance;
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
                    var context = _contextAnalyzer.Analyze(queryText, cursorPosition, correlationId);
                    var cache = await _schemaCache.GetOrLoadAsync(connectionKey, correlationId, cancellationToken).ConfigureAwait(false);

                    var suggestions = new List<ISuggestion>();
                    if (cache != null)
                    {
                        if (!string.IsNullOrWhiteSpace(context.QualifierPrefix))
                        {
                            var qualifier = context.QualifierPrefix;
                            var table = ResolveQualifiedTable(context, cache, qualifier);
                            if (table != null)
                            {
                                suggestions.AddRange(CreateColumnSuggestions(table));
                            }
                        }
                        else
                        {
                            suggestions.AddRange(CreateTableSuggestions(cache.Tables, SuggestionType.Table));
                            suggestions.AddRange(CreateTableSuggestions(cache.Views, SuggestionType.View));
                            suggestions.AddRange(CreateColumnSuggestions(cache.Tables));
                            suggestions.AddRange(CreateColumnSuggestions(cache.Views));
                        }
                    }

                    var typedToken = GetTypedToken(context.PartialInput);
                    var rankedSuggestions = RankSuggestions(suggestions, context, typedToken);
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

        private static IReadOnlyList<ISuggestion> RankSuggestions(
            IEnumerable<ISuggestion> suggestions,
            IAutocompleteContext context,
            string typedToken)
        {
            var clause = context.CurrentClause;
            return suggestions
                .OrderByDescending(suggestion => GetSuggestionScore(suggestion, clause, typedToken))
                .ThenBy(suggestion => suggestion.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetSuggestionScore(ISuggestion suggestion, ClauseType clause, string typedToken)
        {
            var score = suggestion.RelevanceScore;
            score += GetClauseBonus(suggestion.Type, clause);

            if (!string.IsNullOrEmpty(typedToken) &&
                suggestion.DisplayText.StartsWith(typedToken, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            return score;
        }

        private static int GetClauseBonus(SuggestionType type, ClauseType clause)
        {
            switch (clause)
            {
                case ClauseType.From:
                case ClauseType.Join:
                    return type == SuggestionType.Table || type == SuggestionType.View ? 40 : 0;
                case ClauseType.Select:
                case ClauseType.Where:
                case ClauseType.On:
                case ClauseType.GroupBy:
                case ClauseType.OrderBy:
                case ClauseType.Having:
                    return type == SuggestionType.Column ? 40 : 0;
                default:
                    return 0;
            }
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
