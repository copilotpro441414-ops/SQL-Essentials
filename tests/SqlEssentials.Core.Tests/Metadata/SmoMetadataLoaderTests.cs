using System;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class SmoMetadataLoaderTests
    {
        [Fact]
        public async Task LoadAsync_ReturnsReadyCache_WithConnectionKey()
        {
            var loader = new SmoMetadataLoader(NullLogger.Instance);

            var cache = await loader.LoadAsync("conn-A");

            Assert.NotNull(cache);
            Assert.Equal("conn-A", cache.ConnectionKey);
            Assert.Equal(CacheStatus.Ready, cache.Status);
            Assert.Empty(cache.Tables);
            Assert.Empty(cache.Views);
            Assert.Empty(cache.ForeignKeys);
            Assert.Empty(cache.Procedures);
            Assert.Empty(cache.Functions);
        }

        [Fact]
        public async Task LoadAsync_CanceledToken_ThrowsOperationCanceledException()
        {
            var loader = new SmoMetadataLoader(NullLogger.Instance);
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    loader.LoadAsync("conn-A", cancellationToken: cts.Token));
            }
        }

        [Fact]
        public async Task LoadAsync_NullConnectionKey_ThrowsArgumentNullException()
        {
            var logger = new RecordingLogger();
            var loader = new SmoMetadataLoader(logger);

            await Assert.ThrowsAsync<ArgumentNullException>(() => loader.LoadAsync(null));
            Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
        }

        private sealed class RecordingLogger : ILogger
        {
            public System.Collections.Generic.List<(LogLevel Level, string Component, string Message)> Entries { get; }
                = new System.Collections.Generic.List<(LogLevel, string, string)>();

            public bool IsEnabled(LogLevel level) => level != LogLevel.Off;

            public void Log(LogLevel level, string component, string message, Exception exception = null, System.Collections.Generic.IReadOnlyDictionary<string, object> properties = null, string correlationId = null)
            {
                Entries.Add((level, component, message));
            }

            public ILogScope BeginScope(string component, string operation, string correlationId = null, System.Collections.Generic.IReadOnlyDictionary<string, object> properties = null)
            {
                return NullScope.Instance;
            }

            private sealed class NullScope : ILogScope
            {
                public static readonly NullScope Instance = new NullScope();
                public string Component => "Test";
                public string Operation => "Op";
                public string CorrelationId => null;
                public TimeSpan Elapsed => TimeSpan.Zero;
                public void Dispose() { }
            }
        }
    }
}
