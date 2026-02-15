using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Snippets;

namespace SqlEssentials.Core.Models
{
    public sealed class Snippet : ISnippet
    {
        public Snippet(
            string shortcut,
            string name,
            string description,
            string category,
            IReadOnlyList<string> bodyLines,
            IReadOnlyDictionary<int, IPlaceholderDefinition> placeholders,
            bool isBuiltIn)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                throw new ArgumentException("Snippet shortcut cannot be empty.", nameof(shortcut));
            }

            if (bodyLines == null || bodyLines.Count == 0)
            {
                throw new ArgumentException("Snippet body must contain at least one line.", nameof(bodyLines));
            }

            Shortcut = shortcut;
            Name = string.IsNullOrWhiteSpace(name) ? shortcut : name;
            Description = description ?? string.Empty;
            Category = category ?? string.Empty;
            BodyLines = bodyLines;
            Placeholders = placeholders ?? new Dictionary<int, IPlaceholderDefinition>();
            IsBuiltIn = isBuiltIn;
        }

        public string Shortcut { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public IReadOnlyList<string> BodyLines { get; }
        public IReadOnlyDictionary<int, IPlaceholderDefinition> Placeholders { get; }
        public bool IsBuiltIn { get; }

        public string ExpandedBody => string.Join(Environment.NewLine, BodyLines ?? Enumerable.Empty<string>());
    }
}