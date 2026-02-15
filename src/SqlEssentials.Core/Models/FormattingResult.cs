using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Formatting;

namespace SqlEssentials.Core.Models
{
    public sealed class FormattingResult : IFormattingResult
    {
        public FormattingResult(string formattedText, bool success, IReadOnlyList<FormattingError> errors, IReadOnlyList<FormattingError> warnings)
        {
            FormattedText = formattedText ?? string.Empty;
            Success = success;
            Errors = errors ?? Array.Empty<FormattingError>();
            Warnings = warnings ?? Array.Empty<FormattingError>();
        }

        public string FormattedText { get; }
        public bool Success { get; }
        public IReadOnlyList<FormattingError> Errors { get; }
        public IReadOnlyList<FormattingError> Warnings { get; }

        public static FormattingResult SuccessResult(string formattedText)
        {
            return new FormattingResult(formattedText, true, Array.Empty<FormattingError>(), Array.Empty<FormattingError>());
        }

        public static FormattingResult Failure(string originalText, IEnumerable<FormattingError> errors)
        {
            return new FormattingResult(originalText, false, errors?.ToList() ?? new List<FormattingError>(), Array.Empty<FormattingError>());
        }
    }
}
