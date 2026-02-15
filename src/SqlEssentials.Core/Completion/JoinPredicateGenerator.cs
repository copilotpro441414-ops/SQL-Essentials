using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    public sealed class JoinPredicateGenerator
    {
        private readonly ILogger _logger;

        public JoinPredicateGenerator(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IReadOnlyList<JoinSuggestion> Generate(IJoinContext joinContext, IDatabaseCache cache, string correlationId = null)
        {
            if (joinContext == null)
            {
                throw new ArgumentNullException(nameof(joinContext));
            }

            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            var leftTable = ResolveTable(cache, joinContext.LeftTableOrAlias, joinContext.ResolvedLeftTable);
            var rightTable = ResolveTable(cache, joinContext.RightTableOrAlias, joinContext.ResolvedRightTable);

            if (leftTable == null || rightTable == null)
            {
                _logger.Log(LogLevel.Debug, "JoinPredicateGenerator", "Could not resolve join tables", properties: new Dictionary<string, object>
                {
                    { "left", joinContext.LeftTableOrAlias ?? string.Empty },
                    { "right", joinContext.RightTableOrAlias ?? string.Empty }
                }, correlationId: correlationId);

                return Array.Empty<JoinSuggestion>();
            }

            var suggestions = new List<JoinSuggestion>();
            var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var fkMatches = GetForeignKeyMatches(cache, leftTable, rightTable);
            _logger.Log(LogLevel.Debug, "JoinPredicateGenerator", "FK lookup completed", properties: new Dictionary<string, object>
            {
                { "fk_match_count", fkMatches.Count }
            }, correlationId: correlationId);

            foreach (var fkMatch in fkMatches)
            {
                var suggestion = CreateSuggestion(
                    joinContext.LeftTableOrAlias,
                    leftTable.ObjectName,
                    fkMatch.LeftColumns,
                    joinContext.RightTableOrAlias,
                    rightTable.ObjectName,
                    fkMatch.RightColumns,
                    JoinMatchType.ForeignKey,
                    relevanceScore: 200);

                if (dedup.Add(suggestion.InsertionText))
                {
                    suggestions.Add(suggestion);
                    _logger.Log(LogLevel.Debug, "JoinPredicateGenerator", "Generated FK predicate", properties: new Dictionary<string, object>
                    {
                        { "predicate", suggestion.InsertionText }
                    }, correlationId: correlationId);
                }
            }

            var fallbackMatches = GetColumnNameFallbackMatches(leftTable, rightTable);
            _logger.Log(LogLevel.Debug, "JoinPredicateGenerator", "Column name fallback lookup completed", properties: new Dictionary<string, object>
            {
                { "fallback_match_count", fallbackMatches.Count }
            }, correlationId: correlationId);

            foreach (var fallback in fallbackMatches)
            {
                var matchType = fallback.IsExact ? JoinMatchType.ColumnNameExact : JoinMatchType.ColumnNameSimilar;
                var relevance = fallback.IsExact ? 120 : 80;

                var suggestion = CreateSuggestion(
                    joinContext.LeftTableOrAlias,
                    leftTable.ObjectName,
                    new[] { fallback.LeftColumn },
                    joinContext.RightTableOrAlias,
                    rightTable.ObjectName,
                    new[] { fallback.RightColumn },
                    matchType,
                    relevance);

                if (dedup.Add(suggestion.InsertionText))
                {
                    suggestions.Add(suggestion);
                    _logger.Log(LogLevel.Debug, "JoinPredicateGenerator", "Generated fallback predicate", properties: new Dictionary<string, object>
                    {
                        { "predicate", suggestion.InsertionText },
                        { "match_type", matchType.ToString() }
                    }, correlationId: correlationId);
                }
            }

            return suggestions
                .OrderByDescending(s => s.RelevanceScore)
                .ThenBy(s => s.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ITableDefinition ResolveTable(IDatabaseCache cache, string tableOrAlias, string resolvedTableName)
        {
            if (!string.IsNullOrWhiteSpace(resolvedTableName))
            {
                var resolved = cache.FindTable(resolvedTableName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            if (!string.IsNullOrWhiteSpace(tableOrAlias))
            {
                return cache.FindTable(tableOrAlias);
            }

            return null;
        }

        private static List<(IReadOnlyList<string> LeftColumns, IReadOnlyList<string> RightColumns)> GetForeignKeyMatches(
            IDatabaseCache cache,
            ITableDefinition leftTable,
            ITableDefinition rightTable)
        {
            var result = new List<(IReadOnlyList<string>, IReadOnlyList<string>)>();

            foreach (var fk in cache.ForeignKeys)
            {
                var parentIsLeft = IsSameTable(fk.ParentSchema, fk.ParentTable, leftTable);
                var refIsRight = IsSameTable(fk.ReferencedSchema, fk.ReferencedTable, rightTable);
                if (parentIsLeft && refIsRight)
                {
                    result.Add((fk.ParentColumns, fk.ReferencedColumns));
                    continue;
                }

                var parentIsRight = IsSameTable(fk.ParentSchema, fk.ParentTable, rightTable);
                var refIsLeft = IsSameTable(fk.ReferencedSchema, fk.ReferencedTable, leftTable);
                if (parentIsRight && refIsLeft)
                {
                    result.Add((fk.ReferencedColumns, fk.ParentColumns));
                }
            }

            return result;
        }

        private static bool IsSameTable(string schema, string table, ITableDefinition target)
        {
            return schema.Equals(target.SchemaName, StringComparison.OrdinalIgnoreCase)
                && table.Equals(target.ObjectName, StringComparison.OrdinalIgnoreCase);
        }

        private static List<(string LeftColumn, string RightColumn, bool IsExact)> GetColumnNameFallbackMatches(
            ITableDefinition leftTable,
            ITableDefinition rightTable)
        {
            var matches = new List<(string, string, bool)>();
            var leftColumns = leftTable.Columns;
            var rightColumns = rightTable.Columns;

            foreach (var leftColumn in leftColumns)
            {
                foreach (var rightColumn in rightColumns)
                {
                    if (leftColumn.Name.Equals(rightColumn.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add((leftColumn.Name, rightColumn.Name, true));
                        continue;
                    }

                    var leftNormalized = NormalizeForSimilarity(leftColumn.Name);
                    var rightNormalized = NormalizeForSimilarity(rightColumn.Name);
                    if (!string.IsNullOrEmpty(leftNormalized) && leftNormalized.Equals(rightNormalized, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add((leftColumn.Name, rightColumn.Name, false));
                    }
                }
            }

            return matches;
        }

        private static string NormalizeForSimilarity(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return name
                .Replace("_", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Trim();
        }

        private static JoinSuggestion CreateSuggestion(
            string leftAlias,
            string leftTable,
            IReadOnlyList<string> leftColumns,
            string rightAlias,
            string rightTable,
            IReadOnlyList<string> rightColumns,
            JoinMatchType matchType,
            int relevanceScore)
        {
            var leftParts = leftColumns ?? Array.Empty<string>();
            var rightParts = rightColumns ?? Array.Empty<string>();
            var pairCount = Math.Min(leftParts.Count, rightParts.Count);
            var predicates = new List<string>(pairCount);

            for (var i = 0; i < pairCount; i++)
            {
                predicates.Add($"{leftAlias}.{leftParts[i]} = {rightAlias}.{rightParts[i]}");
            }

            var predicateText = string.Join(" AND ", predicates);
            return new JoinSuggestion(
                predicateText,
                predicateText,
                $"JOIN predicate ({matchType})",
                relevanceScore,
                leftAlias,
                leftTable,
                leftParts,
                rightAlias,
                rightTable,
                rightParts,
                matchType);
        }
    }
}