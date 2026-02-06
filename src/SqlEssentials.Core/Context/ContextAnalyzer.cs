using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    public sealed class ContextAnalyzer : IContextAnalyzer
    {
        private readonly TSqlParser _parser;
        private readonly ILogger _logger;

        public ContextAnalyzer(ILogger logger = null)
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
            _logger = logger ?? NullLogger.Instance;
        }

        public IAutocompleteContext Analyze(string queryText, int cursorPosition, string correlationId = null)
        {
            if (queryText == null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            using (var scope = _logger.BeginScope("ContextAnalyzer", "Analyze", correlationId: correlationId))
            {
                var currentClause = GetClauseAtPosition(queryText, cursorPosition);
                var aliases = ExtractAliases(queryText);
                var referencedTables = new List<string>();
                foreach (var alias in aliases.Values)
                {
                    referencedTables.Add(alias.FullyQualifiedName);
                }

                var partialInput = GetPartialInput(queryText, cursorPosition);
                var qualifierPrefix = GetQualifierPrefix(partialInput);

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
                    TriggerReason.Typing);
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
            IList<ParseError> errors;
            var fragment = _parser.Parse(new StringReader(queryText), out errors);
            if (fragment == null)
            {
                return ClauseType.Unknown;
            }

            var visitor = new ClauseDetectionVisitor(cursorPosition);
            fragment.Accept(visitor);
            return visitor.DetectedClause;
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
