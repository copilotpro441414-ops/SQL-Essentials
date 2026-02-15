using System;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class SchemaCacheExpirationTests
    {
        [Fact]
        public async Task ExpiredEntry_ReturnsStaleAndTriggersRefresh()
        {
            var fakeClock = new FakeClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
            var callCount = 0;
            var loader = new CountingLoader(() =>
            {
                callCount++;
                return new TestMetadataBuilder()
                    .AddTable("dbo", "Users", "UserId", "Username")
                    .Build("conn-A", fakeClock.UtcNow, CacheStatus.Ready);
            });

            var cache = new SqlEssentials.Core.Metadata.SchemaCache(loader, NullLogger.Instance, fakeClock)
            {
                TimeToLive = TimeSpan.FromMinutes(30)
            };

            var initial = await cache.GetOrLoadAsync("conn-A");
            Assert.Equal(1, callCount);

            fakeClock.Advance(TimeSpan.FromMinutes(31));
            var stale = await cache.GetOrLoadAsync("conn-A");

            Assert.Same(initial, stale);

            for (var attempt = 0; attempt < 20 && callCount < 2; attempt++)
            {
                await Task.Delay(25);
            }

            Assert.True(callCount >= 2);
        }

        private sealed class CountingLoader : SmoMetadataLoader
        {
            private readonly Func<DatabaseCache> _factory;

            public CountingLoader(Func<DatabaseCache> factory)
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
