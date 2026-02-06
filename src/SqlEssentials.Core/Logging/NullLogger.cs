using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Logging
{
    public sealed class NullLogger : ILogger
    {
        public static readonly ILogger Instance = new NullLogger();

        private NullLogger() { }

        public bool IsEnabled(LogLevel level) => false;

        public void Log(LogLevel level, string component, string message, Exception exception = null, IReadOnlyDictionary<string, object> properties = null, string correlationId = null)
        {
            // Do nothing
        }

        public ILogScope BeginScope(string component, string operation, string correlationId = null, IReadOnlyDictionary<string, object> properties = null)
        {
            return new NullScope(correlationId, component, operation);
        }

        private sealed class NullScope : ILogScope
        {
            public NullScope(string correlationId, string component, string operation)
            {
                CorrelationId = correlationId;
                Component = component;
                Operation = operation;
            }

            public string CorrelationId { get; }
            public string Component { get; }
            public string Operation { get; }
            public TimeSpan Elapsed => TimeSpan.Zero;
            public void Dispose() { }
        }
    }
}
