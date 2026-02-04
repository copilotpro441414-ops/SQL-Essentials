using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public sealed class SchemaCache : ISchemaCache
    {
        private readonly ConcurrentDictionary<string, DatabaseCache> _caches;
        private readonly SmoMetadataLoader _loader;

        public SchemaCache(SmoMetadataLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _caches = new ConcurrentDictionary<string, DatabaseCache>(StringComparer.OrdinalIgnoreCase);
            TimeToLive = TimeSpan.FromMinutes(30);
        }

        public TimeSpan TimeToLive { get; set; }

        public event EventHandler<CacheStatusChangedEventArgs> StatusChanged;

        public CacheStatus GetStatus(string connectionKey)
        {
            if (connectionKey == null)
            {
                return CacheStatus.Empty;
            }

            return _caches.TryGetValue(connectionKey, out var cache) ? cache.Status : CacheStatus.Empty;
        }

        public async Task<IDatabaseCache> GetOrLoadAsync(string connectionKey, CancellationToken cancellationToken = default)
        {
            if (connectionKey == null)
            {
                throw new ArgumentNullException(nameof(connectionKey));
            }

            if (_caches.TryGetValue(connectionKey, out var existing))
            {
                if (!IsExpired(existing))
                {
                    return existing;
                }

                _ = RefreshAsync(connectionKey, cancellationToken);
                return existing;
            }

            return await LoadFreshAsync(connectionKey, cancellationToken).ConfigureAwait(false);
        }

        public async Task RefreshAsync(string connectionKey, CancellationToken cancellationToken = default)
        {
            if (connectionKey == null)
            {
                throw new ArgumentNullException(nameof(connectionKey));
            }

            if (_caches.TryGetValue(connectionKey, out var existing))
            {
                UpdateStatus(connectionKey, existing.Status, CacheStatus.Refreshing);
            }

            var fresh = await _loader.LoadAsync(connectionKey, cancellationToken).ConfigureAwait(false);
            _caches[connectionKey] = fresh;
            UpdateStatus(connectionKey, CacheStatus.Refreshing, CacheStatus.Ready);
        }

        public void Invalidate(string connectionKey)
        {
            if (connectionKey == null)
            {
                return;
            }

            _caches.TryRemove(connectionKey, out _);
        }

        public void InvalidateAll()
        {
            _caches.Clear();
        }

        private async Task<IDatabaseCache> LoadFreshAsync(string connectionKey, CancellationToken cancellationToken)
        {
            UpdateStatus(connectionKey, CacheStatus.Empty, CacheStatus.Loading);

            try
            {
                var cache = await _loader.LoadAsync(connectionKey, cancellationToken).ConfigureAwait(false);
                _caches[connectionKey] = cache;
                UpdateStatus(connectionKey, CacheStatus.Loading, CacheStatus.Ready);
                return cache;
            }
            catch (Exception ex)
            {
                UpdateStatus(connectionKey, CacheStatus.Loading, CacheStatus.Error, ex);
                throw;
            }
        }

        private bool IsExpired(DatabaseCache cache)
        {
            return DateTimeOffset.UtcNow - cache.LastRefreshed > TimeToLive;
        }

        private void UpdateStatus(string connectionKey, CacheStatus oldStatus, CacheStatus newStatus, Exception error = null)
        {
            StatusChanged?.Invoke(this, new CacheStatusChangedEventArgs(connectionKey, oldStatus, newStatus, error));
        }
    }
}
