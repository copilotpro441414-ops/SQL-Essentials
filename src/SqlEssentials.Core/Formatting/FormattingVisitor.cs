using System;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Formatting
{
    public sealed class FormattingVisitor : TSqlFragmentVisitor
    {
        private static readonly string[] ClauseKeywords =
        {
            "SELECT",
            "FROM",
            "WHERE",
            "GROUP BY",
            "HAVING",
            "ORDER BY",
            "INSERT INTO",
            "UPDATE",
            "DELETE",
            "VALUES"
        };

        private readonly IFormattingProfile _profile;

        public FormattingVisitor(IFormattingProfile profile)
        {
            _profile = profile;
        }

        public int TokenCount { get; private set; }

        public override void Visit(TSqlFragment node)
        {
            if (node == null)
            {
                return;
            }

            TokenCount += Math.Max(0, node.FragmentLength);
            base.Visit(node);
        }

        public string Apply(string sql)
        {
            var normalized = NormalizeWhitespace(sql);
            normalized = ApplyKeywordCasing(normalized);
            normalized = ApplyClauseNewLines(normalized);
            normalized = ApplyJoinFormatting(normalized);
            return normalized.Trim();
        }

        private string NormalizeWhitespace(string sql)
        {
            var text = sql?.Replace("\r\n", "\n").Replace('\r', '\n') ?? string.Empty;
            text = Regex.Replace(text, "[\\t ]+", " ");
            text = Regex.Replace(text, " *\n *", "\n");
            text = Regex.Replace(text, "\n{3,}", "\n\n");
            return text;
        }

        private string ApplyKeywordCasing(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            var output = sql;
            var patterns = new[]
            {
                "select", "from", "where", "join", "inner join", "left join", "right join", "full join", "on", "group by", "order by", "having", "insert", "into", "values", "update", "set", "delete", "with", "as"
            };

            foreach (var keyword in patterns)
            {
                var replacement = ConvertCase(keyword);
                output = Regex.Replace(output, $"\\b{Regex.Escape(keyword)}\\b", replacement, RegexOptions.IgnoreCase);
            }

            return output;
        }

        private string ApplyClauseNewLines(string sql)
        {
            var output = sql;
            foreach (var clause in ClauseKeywords)
            {
                var replacement = Environment.NewLine + ConvertCase(clause);
                output = Regex.Replace(output, $"\\s+{Regex.Escape(ConvertCase(clause))}\\b", replacement, RegexOptions.IgnoreCase);
            }

            return output.TrimStart();
        }

        private string ApplyJoinFormatting(string sql)
        {
            var text = sql;
            if (_profile.JoinOnNewLine)
            {
                text = Regex.Replace(text, "\\s+(INNER JOIN|LEFT JOIN|RIGHT JOIN|FULL JOIN|JOIN)\\b", Environment.NewLine + "$1", RegexOptions.IgnoreCase);
            }

            if (_profile.IndentOnPredicate)
            {
                var indent = _profile.IndentStyle == IndentStyle.Tabs
                    ? "\t"
                    : new string(' ', _profile.IndentSize);

                text = Regex.Replace(text, "\\s+ON\\b", Environment.NewLine + indent + ConvertCase("ON"), RegexOptions.IgnoreCase);
            }

            return text;
        }

        private string ConvertCase(string keyword)
        {
            switch (_profile.KeywordCase)
            {
                case KeywordCase.Lower:
                    return keyword.ToLowerInvariant();
                case KeywordCase.PascalCase:
                    var lower = keyword.ToLowerInvariant();
                    return Regex.Replace(lower, "\\b[a-z]", m => m.Value.ToUpperInvariant());
                default:
                    return keyword.ToUpperInvariant();
            }
        }
    }
}
