using System;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class SchemaCacheLifecycleTests
    {
        [Fact]
        public async Task MissThenHit_UsesLoaderOnlyOnce_AndInvalidateResets()
        {
            var callCount = 0;
            var loader = new CountingLoader(() =>
            {
                callCount++;
                return TestMetadataBuilder.CreateSimpleCache("conn-A");
            });

            var cache = new SqlEssentials.Core.Metadata.SchemaCache(loader, NullLogger.Instance, new FakeClock());

            var first = await cache.GetOrLoadAsync("conn-A");
            var second = await cache.GetOrLoadAsync("conn-A");

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal(1, callCount);
            Assert.Equal(CacheStatus.Ready, cache.GetStatus("conn-A"));

            cache.Invalidate("conn-A");
            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-A"));
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
