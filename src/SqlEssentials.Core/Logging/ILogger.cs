using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Logging
{
    public interface ILogger
    {
        bool IsEnabled(LogLevel level);

        void Log(
            LogLevel level,
            string component,
            string message,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            string correlationId = null);

        ILogScope BeginScope(
            string component,
            string operation,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null);
    }
}
