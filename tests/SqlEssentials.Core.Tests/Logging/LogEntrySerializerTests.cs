using System;
using Newtonsoft.Json.Linq;
using SqlEssentials.Core.Logging;
using Xunit;

namespace SqlEssentials.Core.Tests.Logging
{
    public sealed class LogEntrySerializerTests
    {
        [Fact]
        public void Serialize_UsesCamelCase_AndOmitsNulls()
        {
            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.Parse("2026-02-15T12:00:00Z"),
                Level = LogLevel.Warning,
                Component = "CompletionEngine",
                Message = "Test message",
                CorrelationId = "corr-1"
            };

            var json = LogEntrySerializer.Serialize(entry);
            var parsed = JObject.Parse(json);

            Assert.Equal("Warning", parsed["level"]?.Value<string>());
            Assert.Equal("CompletionEngine", parsed["component"]?.Value<string>());
            Assert.Equal("Test message", parsed["message"]?.Value<string>());
            Assert.Equal("corr-1", parsed["correlationId"]?.Value<string>());
            Assert.NotNull(parsed["timestamp"]);
            Assert.Null(parsed["exception"]);
            Assert.Null(parsed["properties"]);
        }

        [Fact]
        public void Serialize_FormatsExceptionObject()
        {
            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Error,
                Component = "SchemaCache",
                Message = "Failure",
                Exception = new LogExceptionDetails
                {
                    Type = "System.InvalidOperationException",
                    Message = "Invalid state",
                    StackTrace = "stack"
                }
            };

            var json = LogEntrySerializer.Serialize(entry);
            var parsed = JObject.Parse(json);

            Assert.Equal("System.InvalidOperationException", parsed["exception"]?["type"]?.Value<string>());
            Assert.Equal("Invalid state", parsed["exception"]?["message"]?.Value<string>());
            Assert.Equal("stack", parsed["exception"]?["stackTrace"]?.Value<string>());
        }
    }
}
