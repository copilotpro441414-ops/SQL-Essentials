using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    public sealed class SnippetExpansionSession
    {
        public SnippetExpansionSession(
            Snippet snippet,
            int currentPlaceholderIndex,
            IReadOnlyList<PlaceholderSpan> placeholderSpans,
            bool isComplete)
        {
            Snippet = snippet ?? throw new ArgumentNullException(nameof(snippet));
            CurrentPlaceholderIndex = currentPlaceholderIndex;
            PlaceholderSpans = placeholderSpans ?? throw new ArgumentNullException(nameof(placeholderSpans));
            IsComplete = isComplete;
        }

        public Snippet Snippet { get; }
        public int CurrentPlaceholderIndex { get; }
        public IReadOnlyList<PlaceholderSpan> PlaceholderSpans { get; }
        public bool IsComplete { get; }

        public PlaceholderSpan CurrentSpan =>
            !IsComplete && CurrentPlaceholderIndex >= 0 && CurrentPlaceholderIndex < PlaceholderSpans.Count
                ? PlaceholderSpans[CurrentPlaceholderIndex]
                : null;

        public SnippetExpansionSession MoveNext()
        {
            if (IsComplete || PlaceholderSpans.Count == 0)
            {
                return this;
            }

            var next = CurrentPlaceholderIndex + 1;
            if (next >= PlaceholderSpans.Count)
            {
                return new SnippetExpansionSession(Snippet, CurrentPlaceholderIndex, PlaceholderSpans, true);
            }

            return new SnippetExpansionSession(Snippet, next, PlaceholderSpans, false);
        }

        public SnippetExpansionSession MovePrevious()
        {
            if (PlaceholderSpans.Count == 0)
            {
                return this;
            }

            var previous = CurrentPlaceholderIndex - 1;
            if (previous < 0)
            {
                previous = 0;
            }

            return new SnippetExpansionSession(Snippet, previous, PlaceholderSpans, false);
        }
    }
}