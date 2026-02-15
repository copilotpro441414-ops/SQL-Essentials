using System;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class SchemaCacheRefreshFailureTests
    {
        [Fact]
        public async Task RefreshFailure_KeepsStaleCacheReady()
        {
            var fakeClock = new FakeClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
            var loadCount = 0;
            var loader = new FlakyLoader(() =>
            {
                loadCount++;
                if (loadCount == 1)
                {
                    return new TestMetadataBuilder()
                        .AddTable("dbo", "Users", "UserId", "Username")
                        .Build("conn-A", fakeClock.UtcNow, CacheStatus.Ready);
                }

                throw new InvalidOperationException("refresh failed");
            });

            var cache = new SqlEssentials.Core.Metadata.SchemaCache(loader, NullLogger.Instance, fakeClock)
            {
                TimeToLive = TimeSpan.FromMinutes(30)
            };

            var initial = await cache.GetOrLoadAsync("conn-A");
            fakeClock.Advance(TimeSpan.FromMinutes(31));

            var stale = await cache.GetOrLoadAsync("conn-A");
            Assert.Same(initial, stale);

            await Task.Delay(50);
            Assert.Equal(CacheStatus.Ready, cache.GetStatus("conn-A"));
        }

        private sealed class FlakyLoader : SmoMetadataLoader
        {
            private readonly Func<DatabaseCache> _factory;

            public FlakyLoader(Func<DatabaseCache> factory)
            {
                _factory = factory;
            }

            public override Task<DatabaseCache> LoadAsync(string connectionKey, string correlationId = null, System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_factory());
            }
        }
    }
}
