using System.Collections.Generic;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Formatting
{
    public interface IFormattingResult
    {
        string FormattedText { get; }
        bool Success { get; }
        IReadOnlyList<FormattingError> Errors { get; }
        IReadOnlyList<FormattingError> Warnings { get; }
    }
}
