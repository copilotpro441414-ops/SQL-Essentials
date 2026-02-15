using System;
using System.Collections.Generic;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Core.Tests.Shared
{
    public sealed class RecordingLogger : ILogger
    {
        private readonly LogLevel _minEnabledLevel;

        public RecordingLogger(LogLevel minEnabledLevel = LogLevel.Trace)
        {
            _minEnabledLevel = minEnabledLevel;
        }

        public List<(LogLevel Level, string Component, string Message)> Entries { get; } = new List<(LogLevel, string, string)>();

        public bool IsEnabled(LogLevel level)
        {
            return level != LogLevel.Off && level <= _minEnabledLevel;
        }

        public void Log(LogLevel level, string component, string message, Exception exception = null, IReadOnlyDictionary<string, object> properties = null, string correlationId = null)
        {
            if (!IsEnabled(level))
            {
                return;
            }

            Entries.Add((level, component, message));
        }

        public ILogScope BeginScope(string component, string operation, string correlationId = null, IReadOnlyDictionary<string, object> properties = null)
        {
            return new LogScope(this, component, operation, correlationId, properties);
        }
    }
}
