using System;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Tests.Shared
{
    public sealed class FakeSchemaCache : ISchemaCache
    {
        private readonly IDatabaseCache _cache;

        public FakeSchemaCache(IDatabaseCache cache)
        {
            _cache = cache;
        }

        public CacheStatus GetStatus(string connectionKey) => _cache?.Status ?? CacheStatus.Empty;

        public Task<IDatabaseCache> GetOrLoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache);
        }

        public Task RefreshAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Invalidate(string connectionKey)
        {
        }

        public void InvalidateAll()
        {
        }

        public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(30);

        public event EventHandler<CacheStatusChangedEventArgs> StatusChanged
        {
            add { }
            remove { }
        }
    }
}
