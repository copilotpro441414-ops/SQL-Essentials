using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    public sealed class ClauseDetectionVisitor : TSqlFragmentVisitor
    {
        private readonly int _cursorPosition;
        private readonly ILogger _logger;
        private readonly string _correlationId;
        private int _bestMatchLength = int.MaxValue;

        public ClauseDetectionVisitor(int cursorPosition, ILogger logger = null, string correlationId = null)
        {
            _cursorPosition = cursorPosition;
            _logger = logger ?? NullLogger.Instance;
            _correlationId = correlationId;
        }

        public ClauseType DetectedClause { get; private set; } = ClauseType.Unknown;

        public override void Visit(QuerySpecification node)
        {
            if (node.SelectElements != null)
            {
                foreach (var element in node.SelectElements)
                {
                    TrySetClause(ClauseType.Select, element);
                }
            }

            if (node.FromClause != null)
            {
                TrySetClause(ClauseType.From, node.FromClause);
            }

            if (node.WhereClause != null)
            {
                TrySetClause(ClauseType.Where, node.WhereClause);
            }

            if (node.GroupByClause != null)
            {
                TrySetClause(ClauseType.GroupBy, node.GroupByClause);
            }

            if (node.HavingClause != null)
            {
                TrySetClause(ClauseType.Having, node.HavingClause);
            }

            base.Visit(node);
        }

        public override void Visit(OrderByClause node)
        {
            TrySetClause(ClauseType.OrderBy, node);
            base.Visit(node);
        }

        public override void Visit(QualifiedJoin node)
        {
            if (node.SearchCondition != null && IsInFragment(node.SearchCondition))
            {
                TrySetClause(ClauseType.On, node.SearchCondition);
            }
            else
            {
                TrySetClause(ClauseType.Join, node);
            }

            base.Visit(node);
        }

        private void TrySetClause(ClauseType clause, TSqlFragment fragment)
        {
            if (!IsInFragment(fragment))
            {
                return;
            }

            if (fragment.FragmentLength < _bestMatchLength)
            {
                _bestMatchLength = fragment.FragmentLength;
                DetectedClause = clause;
                _logger.Log(LogLevel.Trace, "ClauseDetectionVisitor", "Detected clause", properties: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "clause", clause.ToString() },
                    { "cursor_position", _cursorPosition },
                    { "fragment_start", fragment.StartOffset },
                    { "fragment_length", fragment.FragmentLength }
                }, correlationId: _correlationId);
            }
        }

        private bool IsInFragment(TSqlFragment fragment)
        {
            if (fragment == null)
            {
                return false;
            }

            var start = fragment.StartOffset;
            var end = start + fragment.FragmentLength;
            return _cursorPosition >= start && _cursorPosition <= end;
        }
    }
}
