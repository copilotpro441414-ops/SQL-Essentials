using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    /// <summary>
    /// Component for classifying SQL clauses based on cursor position.
    /// Isolated adapter over parser traversal for testability.
    /// </summary>
    public sealed class ClauseClassifier
    {
        private readonly TSqlParser _parser;
        private readonly ILogger _logger;

        public ClauseClassifier(TSqlParser parser = null, ILogger logger = null)
        {
            _parser = parser ?? new TSql160Parser(initialQuotedIdentifiers: false);
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Determines the SQL clause type at the given cursor position in the query text.
        /// </summary>
        /// <param name="queryText">The SQL query text</param>
        /// <param name="cursorPosition">The cursor position within the query</param>
        /// <returns>The detected clause type, or ClauseType.Unknown if detection fails</returns>
        public ClauseType ClassifyClause(string queryText, int cursorPosition, string correlationId = null)
        {
            if (queryText == null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            if (cursorPosition < 0 || cursorPosition > queryText.Length)
            {
                return ClauseType.Unknown;
            }

            IList<ParseError> errors;
            var fragment = _parser.Parse(new StringReader(queryText), out errors);
            if (fragment == null)
            {
                return ClauseType.Unknown;
            }

            var visitor = new ClauseDetectionVisitor(cursorPosition, _logger, correlationId);
            fragment.Accept(visitor);
            return visitor.DetectedClause;
        }
    }
}
