using System;
using System.Linq;
using System.Threading;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Logging
{
    public sealed class LogScopeTests
    {
        [Fact]
        public void Scope_EmitsBeginAndEnd_WithElapsed()
        {
            var logger = new RecordingLogger(LogLevel.Trace);

            using (var scope = new LogScope(logger, "Comp", "Op", "corr-1"))
            {
                Thread.Sleep(10);
                Assert.Equal("Comp", scope.Component);
                Assert.Equal("Op", scope.Operation);
                Assert.Equal("corr-1", scope.CorrelationId);
            }

            Assert.True(logger.Entries.Count >= 2);
            Assert.Contains(logger.Entries, e => e.Message.Contains("ScopeBegin"));
            Assert.Contains(logger.Entries, e => e.Message.Contains("ScopeEnd"));
        }

        [Fact]
        public void Scope_NullLogger_DoesNotThrow()
        {
            using (var scope = new LogScope(null, "Comp", "Op"))
            {
                Assert.NotNull(scope);
            }
        }
    }
}
