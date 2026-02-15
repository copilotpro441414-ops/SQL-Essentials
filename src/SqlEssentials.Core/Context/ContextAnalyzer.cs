using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    public sealed class ContextAnalyzer : IContextAnalyzer
    {
        private readonly TSqlParser _parser;
        private readonly ILogger _logger;
        private readonly ClauseClassifier _clauseClassifier;

        public ContextAnalyzer(ILogger logger = null, ClauseClassifier clauseClassifier = null)
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
            _logger = logger ?? NullLogger.Instance;
            _clauseClassifier = clauseClassifier ?? new ClauseClassifier(_parser);
        }

        public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null)
        {
            if (queryText == null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            using (var scope = _logger.BeginScope("ContextAnalyzer", "Analyze", correlationId: correlationId))
            {
                var currentClause = _clauseClassifier.ClassifyClause(queryText, cursorPosition, correlationId);
                var aliases = ExtractAliases(queryText);
                var referencedTables = new List<string>();
                foreach (var alias in aliases.Values)
                {
                    referencedTables.Add(alias.FullyQualifiedName);
                }

                var partialInput = GetPartialInput(queryText, cursorPosition);
                var qualifierPrefix = GetQualifierPrefix(partialInput);
                var joinContext = GetJoinContext(queryText, cursorPosition, currentClause, aliases);

                _logger.Log(LogLevel.Trace, "ContextAnalyzer", "Analysis complete", properties: new Dictionary<string, object>
                {
                    { "clause", currentClause.ToString() },
                    { "alias_count", aliases.Count },
                    { "qualifier", qualifierPrefix ?? string.Empty }
                }, correlationId: correlationId);

                return new AutocompleteContext(
                    queryText,
                    cursorPosition,
                    currentClause,
                    partialInput,
                    qualifierPrefix,
                    aliases,
                    referencedTables,
                    TriggerReason.Typing,
                    joinContext);
            }
        }

        public IReadOnlyDictionary<string, IAliasBinding> ExtractAliases(string queryText)
        {
            if (queryText == null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            IList<ParseError> errors;
            TSqlFragment fragment = _parser.Parse(new StringReader(queryText), out errors);

            if (fragment == null)
            {
                return new Dictionary<string, IAliasBinding>(StringComparer.OrdinalIgnoreCase);
            }

            var visitor = new AliasVisitor();
            fragment.Accept(visitor);
            return visitor.Aliases;
        }

        public bool IsInNonCodeContext(string queryText, int cursorPosition)
        {
            if (queryText == null)
            {
                return false;
            }

            IList<ParseError> errors;
            var tokens = _parser.GetTokenStream(new StringReader(queryText), out errors);

            foreach (var token in tokens)
            {
                if (cursorPosition < token.Offset || cursorPosition >= token.Offset + token.Text.Length)
                {
                    continue;
                }

                if (token.TokenType == TSqlTokenType.MultilineComment ||
                    token.TokenType == TSqlTokenType.SingleLineComment ||
                    token.TokenType == TSqlTokenType.AsciiStringLiteral ||
                    token.TokenType == TSqlTokenType.UnicodeStringLiteral)
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        private static string GetPartialInput(string queryText, int cursorPosition)
        {
            if (cursorPosition <= 0 || cursorPosition > queryText.Length)
            {
                return string.Empty;
            }

            var index = cursorPosition - 1;
            while (index >= 0)
            {
                var ch = queryText[index];
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '[' || ch == ']')
                {
                    index--;
                    continue;
                }

                break;
            }

            return queryText.Substring(index + 1, cursorPosition - index - 1);
        }

        private static string GetQualifierPrefix(string partialInput)
        {
            if (string.IsNullOrWhiteSpace(partialInput))
            {
                return string.Empty;
            }

            var dotIndex = partialInput.LastIndexOf('.');
            if (dotIndex <= 0)
            {
                return string.Empty;
            }

            return partialInput.Substring(0, dotIndex);
        }

        private ClauseType GetClauseAtPosition(string queryText, int cursorPosition)
        {
            // Deprecated: Use ClauseClassifier.ClassifyClause instead
            // Kept temporarily for compatibility
            return _clauseClassifier.ClassifyClause(queryText, cursorPosition);
        }

        private static IJoinContext GetJoinContext(
            string queryText,
            int cursorPosition,
            ClauseType currentClause,
            IReadOnlyDictionary<string, IAliasBinding> aliases)
        {
            if (currentClause != ClauseType.On || string.IsNullOrWhiteSpace(queryText) || cursorPosition <= 0)
            {
                return null;
            }

            var prefix = queryText.Substring(0, Math.Min(cursorPosition, queryText.Length));
            var joinMatches = Regex.Matches(
                prefix,
                "\\bjoin\\s+([^\\s]+)(?:\\s+(?:as\\s+)?([A-Za-z_][\\w]*))?\\s+\\bon\\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (joinMatches.Count == 0)
            {
                return null;
            }

            var currentJoin = joinMatches[joinMatches.Count - 1];
            var rightTableToken = currentJoin.Groups[1].Value;
            var rightAlias = currentJoin.Groups[2].Success && !string.IsNullOrWhiteSpace(currentJoin.Groups[2].Value)
                ? currentJoin.Groups[2].Value
                : GetObjectName(rightTableToken);

            var textBeforeJoin = prefix.Substring(0, currentJoin.Index);
            var sideMatches = Regex.Matches(
                textBeforeJoin,
                "\\b(?:from|join)\\s+([^\\s]+)(?:\\s+(?:as\\s+)?([A-Za-z_][\\w]*))?",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (sideMatches.Count == 0)
            {
                return null;
            }

            var leftSide = sideMatches[sideMatches.Count - 1];
            var leftTableToken = leftSide.Groups[1].Value;
            var leftAlias = leftSide.Groups[2].Success && !string.IsNullOrWhiteSpace(leftSide.Groups[2].Value)
                ? leftSide.Groups[2].Value
                : GetObjectName(leftTableToken);

            var resolvedLeft = ResolveAliasOrTable(leftAlias, aliases);
            var resolvedRight = ResolveAliasOrTable(rightAlias, aliases);

            return new JoinContext(leftAlias, rightAlias, resolvedLeft, resolvedRight);
        }

        private static string ResolveAliasOrTable(string aliasOrTable, IReadOnlyDictionary<string, IAliasBinding> aliases)
        {
            if (string.IsNullOrWhiteSpace(aliasOrTable))
            {
                return string.Empty;
            }

            if (aliases != null && aliases.TryGetValue(aliasOrTable, out var binding))
            {
                return binding.TableName;
            }

            return GetObjectName(aliasOrTable);
        }

        private static string GetObjectName(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var cleaned = token.Trim().Trim(',').Replace("[", string.Empty).Replace("]", string.Empty);
            var lastDot = cleaned.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < cleaned.Length - 1)
            {
                return cleaned.Substring(lastDot + 1);
            }

            return cleaned;
        }

        private sealed class AliasVisitor : TSqlFragmentVisitor
        {
            public Dictionary<string, IAliasBinding> Aliases { get; } = new Dictionary<string, IAliasBinding>(StringComparer.OrdinalIgnoreCase);

            public override void Visit(NamedTableReference node)
            {
                var schema = node.SchemaObject.SchemaIdentifier?.Value ?? "dbo";
                var name = node.SchemaObject.BaseIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                if (node.Alias != null && !string.IsNullOrWhiteSpace(node.Alias.Value))
                {
                    var alias = node.Alias.Value;
                    Aliases[alias] = new AliasBinding(alias, schema, name, node.StartOffset);
                }
            }
        }
    }
}
