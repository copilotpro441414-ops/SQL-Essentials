using SqlEssentials.Core.Logging;
using Xunit;

namespace SqlEssentials.Core.Tests.Logging
{
    public sealed class NullLoggerTests
    {
        [Fact]
        public void NullLogger_IsNoOp_ForLogAndScope()
        {
            var logger = NullLogger.Instance;

            logger.Log(LogLevel.Error, "Comp", "Err");
            logger.Log(LogLevel.Trace, "Comp", "Trace");

            using (var scope = logger.BeginScope("Comp", "Op", "corr-1"))
            {
                Assert.NotNull(scope);
                Assert.Equal("corr-1", scope.CorrelationId);
            }
        }

        [Fact]
        public void NullLogger_IsEnabled_AlwaysFalse()
        {
            var logger = NullLogger.Instance;

            Assert.False(logger.IsEnabled(LogLevel.Error));
            Assert.False(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.Info));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Trace));
        }
    }
}
