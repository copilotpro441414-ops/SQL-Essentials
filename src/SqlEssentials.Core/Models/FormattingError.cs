using System;

namespace SqlEssentials.Core.Models
{
    public sealed class FormattingError
    {
        public FormattingError(int line, int column, string message, bool isWarning)
        {
            Line = Math.Max(0, line);
            Column = Math.Max(0, column);
            Message = message ?? string.Empty;
            IsWarning = isWarning;
        }

        public int Line { get; }
        public int Column { get; }
        public string Message { get; }
        public bool IsWarning { get; }
    }
}
