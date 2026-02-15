using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Logging
{
    public sealed class LoggerLevelFilteringTests
    {
        [Fact]
        public void RecordingLogger_FiltersByConfiguredLevel()
        {
            var logger = new RecordingLogger(LogLevel.Info);

            logger.Log(LogLevel.Error, "Comp", "error");
            logger.Log(LogLevel.Warning, "Comp", "warn");
            logger.Log(LogLevel.Info, "Comp", "info");
            logger.Log(LogLevel.Debug, "Comp", "debug");
            logger.Log(LogLevel.Trace, "Comp", "trace");

            Assert.Contains(logger.Entries, e => e.Message == "error");
            Assert.Contains(logger.Entries, e => e.Message == "warn");
            Assert.Contains(logger.Entries, e => e.Message == "info");
            Assert.DoesNotContain(logger.Entries, e => e.Message == "debug");
            Assert.DoesNotContain(logger.Entries, e => e.Message == "trace");
        }
    }
}
