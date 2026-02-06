using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Logging
{
    public sealed class FileLogger : ILogger, IDisposable
    {
        private readonly LogLevel _configuredLevel;
        private readonly LogFileOptions _options;
        private readonly LogWriteQueue _queue;
        private readonly object _fileLock = new object();

        public FileLogger(LogLevel configuredLevel, LogFileOptions options)
        {
            _configuredLevel = configuredLevel;
            _options = options;
            _queue = new LogWriteQueue(WriteToDisk);
        }

        public bool IsEnabled(LogLevel level)
        {
            if (_configuredLevel == LogLevel.Off) return false;
            if (level == LogLevel.Off) return false;
            return level <= _configuredLevel;
        }

        public void Log(LogLevel level, string component, string message, Exception exception = null, IReadOnlyDictionary<string, object> properties = null, string correlationId = null)
        {
            if (!IsEnabled(level)) return;

            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = level,
                Component = component,
                Message = message,
                CorrelationId = correlationId,
                Properties = properties
            };

            if (exception != null)
            {
                entry.Exception = new LogExceptionDetails
                {
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                };
            }

            _queue.Enqueue(entry);
        }

        public ILogScope BeginScope(string component, string operation, string correlationId = null, IReadOnlyDictionary<string, object> properties = null)
        {
            return new LogScope(this, component, operation, correlationId, properties);
        }

        private void WriteToDisk(LogEntry entry)
        {
            lock (_fileLock)
            {
                try
                {
                    var line = LogEntrySerializer.Serialize(entry);
                    EnsureDirectoryExists();
                    RotateIfNeeded();
                    File.AppendAllLines(_options.LogFilePath, new[] { line }, Encoding.UTF8);
                }
                catch
                {
                    // Fail silently
                }
            }
        }

        private void EnsureDirectoryExists()
        {
            var dir = Path.GetDirectoryName(_options.LogFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(_options.LogFilePath)) return;

                var info = new FileInfo(_options.LogFilePath);
                if (info.Length >= _options.MaxFileSizeBytes)
                {
                    var backup = LogPathBuilder.GetBackupFilePath(_options.LogFilePath);
                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }
                    File.Move(_options.LogFilePath, backup);
                }
            }
            catch
            {
                // Ignore rotation errors
            }
        }

        public void Dispose()
        {
            _queue.Dispose();
        }
    }
}
