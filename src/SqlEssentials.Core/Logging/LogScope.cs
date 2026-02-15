using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SqlEssentials.Core.Logging
{
    public sealed class LogScope : ILogScope
    {
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public LogScope(
            ILogger logger,
            string component,
            string operation,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            _logger = logger ?? NullLogger.Instance;
            Component = component;
            Operation = operation;
            CorrelationId = correlationId;
            _stopwatch = Stopwatch.StartNew();

            _logger.Log(LogLevel.Trace, component, $"ScopeBegin: {operation}", correlationId: correlationId, properties: properties);
        }

        public string CorrelationId { get; }
        public string Component { get; }
        public string Operation { get; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void Dispose()
        {
            _stopwatch.Stop();
            var props = new Dictionary<string, object> { { "elapsed_ms", Elapsed.TotalMilliseconds } };
            _logger.Log(LogLevel.Trace, Component, $"ScopeEnd: {Operation}", correlationId: CorrelationId, properties: props);
        }
    }
}
