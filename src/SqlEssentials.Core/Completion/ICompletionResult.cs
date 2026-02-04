using System.Collections.Generic;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    public interface ICompletionResult
    {
        IReadOnlyList<ISuggestion> Suggestions { get; }
        TextSpan ApplicableSpan { get; }
        bool ShouldFilter { get; }
        int PreselectedIndex { get; }
    }
}
