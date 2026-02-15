using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Snippets;

namespace SqlEssentials.Extension.Snippets
{
    public sealed class SnippetExpander
    {
        private static readonly Regex PlaceholderRegex =
            new Regex("\\$\\{(\\d+)(?::([^}]*))?\\}|\\$(\\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ILogger _logger;

        public SnippetExpander(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public SnippetExpansionSession Expand(ISnippet snippet, int insertionStart, string correlationId = null)
        {
            if (snippet == null)
            {
                throw new ArgumentNullException(nameof(snippet));
            }

            var source = snippet.ExpandedBody ?? string.Empty;
            var output = new StringBuilder(source.Length);
            var spans = new List<PlaceholderSpan>();
            var lastIndex = 0;

            foreach (Match match in PlaceholderRegex.Matches(source))
            {
                output.Append(source.Substring(lastIndex, match.Index - lastIndex));

                var indexToken = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
                if (!int.TryParse(indexToken, out var placeholderIndex))
                {
                    output.Append(match.Value);
                    lastIndex = match.Index + match.Length;
                    continue;
                }

                var replacement = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
                var start = insertionStart + output.Length;
                output.Append(replacement);

                if (placeholderIndex > 0)
                {
                    spans.Add(new PlaceholderSpan(placeholderIndex, start, replacement.Length, replacement));
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < source.Length)
            {
                output.Append(source.Substring(lastIndex));
            }

            var orderedSpans = spans
                .OrderBy(span => span.PlaceholderIndex)
                .ThenBy(span => span.StartPosition)
                .ToList();

            _logger.Log(LogLevel.Trace, "SnippetExpander", "Snippet expansion triggered", properties: new Dictionary<string, object>
            {
                { "shortcut", snippet.Shortcut ?? string.Empty },
                { "placeholder_count", orderedSpans.Count }
            }, correlationId: correlationId);

            var concreteSnippet = snippet as Snippet ?? new Snippet(
                snippet.Shortcut,
                snippet.Name,
                snippet.Description,
                snippet.Category,
                output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None),
                snippet.Placeholders,
                snippet.IsBuiltIn);

            return new SnippetExpansionSession(
                concreteSnippet,
                orderedSpans.Count > 0 ? 0 : -1,
                orderedSpans,
                orderedSpans.Count == 0);
        }

        public SnippetExpansionSession MoveNext(SnippetExpansionSession session, string correlationId = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var next = session.MoveNext();
            _logger.Log(LogLevel.Trace, "SnippetExpander", "Moved to next placeholder", properties: new Dictionary<string, object>
            {
                { "from_index", session.CurrentPlaceholderIndex },
                { "to_index", next.CurrentPlaceholderIndex },
                { "is_complete", next.IsComplete }
            }, correlationId: correlationId);

            return next;
        }

        public SnippetExpansionSession MovePrevious(SnippetExpansionSession session, string correlationId = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var previous = session.MovePrevious();
            _logger.Log(LogLevel.Trace, "SnippetExpander", "Moved to previous placeholder", properties: new Dictionary<string, object>
            {
                { "from_index", session.CurrentPlaceholderIndex },
                { "to_index", previous.CurrentPlaceholderIndex }
            }, correlationId: correlationId);

            return previous;
        }
    }
}