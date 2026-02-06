using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Logging
{
    public sealed class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Component { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public IReadOnlyDictionary<string, object> Properties { get; set; }
        public LogExceptionDetails Exception { get; set; }
    }

    public sealed class LogExceptionDetails
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
