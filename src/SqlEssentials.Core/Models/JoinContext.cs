using System;
using SqlEssentials.Core.Context;

namespace SqlEssentials.Core.Models
{
    public sealed class JoinContext : IJoinContext
    {
        public JoinContext(
            string leftTableOrAlias,
            string rightTableOrAlias,
            string resolvedLeftTable,
            string resolvedRightTable)
        {
            LeftTableOrAlias = leftTableOrAlias ?? throw new ArgumentNullException(nameof(leftTableOrAlias));
            RightTableOrAlias = rightTableOrAlias ?? throw new ArgumentNullException(nameof(rightTableOrAlias));
            ResolvedLeftTable = resolvedLeftTable;
            ResolvedRightTable = resolvedRightTable;
        }

        public string LeftTableOrAlias { get; }
        public string RightTableOrAlias { get; }
        public string ResolvedLeftTable { get; }
        public string ResolvedRightTable { get; }
    }
}