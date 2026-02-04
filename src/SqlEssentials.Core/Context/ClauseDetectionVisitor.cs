using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Context
{
    public sealed class ClauseDetectionVisitor : TSqlFragmentVisitor
    {
        private readonly int _cursorPosition;
        private int _bestMatchLength = int.MaxValue;

        public ClauseDetectionVisitor(int cursorPosition)
        {
            _cursorPosition = cursorPosition;
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
