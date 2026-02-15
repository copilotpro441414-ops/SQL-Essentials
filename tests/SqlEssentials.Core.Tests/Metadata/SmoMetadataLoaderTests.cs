using System;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
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
    }
}
