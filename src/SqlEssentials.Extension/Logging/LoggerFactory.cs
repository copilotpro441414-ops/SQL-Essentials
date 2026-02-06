using System;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Logging
{
    public static class LoggerFactory
    {
        public static ILogger Create()
        {
            var levelStr = Environment.GetEnvironmentVariable(LoggingConstants.LogLevelEnvVar);
            if (string.IsNullOrWhiteSpace(levelStr) || !Enum.TryParse<LogLevel>(levelStr, true, out var level))
            {
                level = LogLevel.Off;
            }

            if (level == LogLevel.Off)
            {
                return NullLogger.Instance;
            }

            var maxMbStr = Environment.GetEnvironmentVariable(LoggingConstants.LogMaxMbEnvVar);
            if (string.IsNullOrWhiteSpace(maxMbStr) || !int.TryParse(maxMbStr, out var maxMb))
            {
                maxMb = LoggingConstants.DefaultMaxFileSizeMb;
            }

            var options = new LogFileOptions
            {
                LogFilePath = LogPathBuilder.GetLogFilePath(),
                MaxFileSizeMb = maxMb
            };

            return new FileLogger(level, options);
        }
    }
}
