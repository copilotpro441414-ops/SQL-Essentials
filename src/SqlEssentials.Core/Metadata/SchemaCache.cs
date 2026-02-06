using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public sealed class SchemaCache : ISchemaCache
    {
        private readonly ConcurrentDictionary<string, DatabaseCache> _caches;
        private readonly SmoMetadataLoader _loader;
        private readonly ILogger _logger;

        public SchemaCache(SmoMetadataLoader loader, ILogger logger = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _logger = logger ?? NullLogger.Instance;
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

        public async Task<IDatabaseCache> GetOrLoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            if (connectionKey == null)
            {
                throw new ArgumentNullException(nameof(connectionKey));
            }

            using (var scope = _logger.BeginScope("SchemaCache", "GetOrLoad", correlationId: correlationId))
            {
                if (_caches.TryGetValue(connectionKey, out var existing))
                {
                    if (!IsExpired(existing))
                    {
                        _logger.Log(LogLevel.Trace, "SchemaCache", $"Cache hit for {connectionKey}", correlationId: correlationId);
                        return existing;
                    }

                    _logger.Log(LogLevel.Info, "SchemaCache", $"Cache expired for {connectionKey}, refreshing", correlationId: correlationId);
                    _ = RefreshAsync(connectionKey, correlationId, cancellationToken);
                    return existing;
                }

                _logger.Log(LogLevel.Info, "SchemaCache", $"Cache miss for {connectionKey}, loading", correlationId: correlationId);
                return await LoadFreshAsync(connectionKey, correlationId, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task RefreshAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            if (connectionKey == null)
            {
                throw new ArgumentNullException(nameof(connectionKey));
            }

            using (var scope = _logger.BeginScope("SchemaCache", "Refresh", correlationId: correlationId))
            {
                _logger.Log(LogLevel.Info, "SchemaCache", $"Refreshing cache for {connectionKey}", correlationId: correlationId);
                if (_caches.TryGetValue(connectionKey, out var existing))
                {
                    UpdateStatus(connectionKey, existing.Status, CacheStatus.Refreshing);
                }

                try
                {
                    var fresh = await _loader.LoadAsync(connectionKey, correlationId, cancellationToken).ConfigureAwait(false);
                    _caches[connectionKey] = fresh;
                    UpdateStatus(connectionKey, CacheStatus.Refreshing, CacheStatus.Ready);
                    _logger.Log(LogLevel.Info, "SchemaCache", $"Refreshed cache for {connectionKey}", correlationId: correlationId);
                }
                catch (Exception ex)
                {
                    UpdateStatus(connectionKey, CacheStatus.Refreshing, _caches.ContainsKey(connectionKey) ? CacheStatus.Ready : CacheStatus.Empty);
                    _logger.Log(LogLevel.Error, "SchemaCache", $"Failed to refresh cache for {connectionKey}", ex, correlationId: correlationId);
                }
            }
        }

        public void Invalidate(string connectionKey)
        {
            if (connectionKey == null)
            {
                return;
            }

            _logger.Log(LogLevel.Info, "SchemaCache", $"Invalidating cache for {connectionKey}");
            _caches.TryRemove(connectionKey, out _);
        }

        public void InvalidateAll()
        {
            _logger.Log(LogLevel.Info, "SchemaCache", "Invalidating all caches");
            _caches.Clear();
        }

        private async Task<IDatabaseCache> LoadFreshAsync(string connectionKey, string correlationId, CancellationToken cancellationToken)
        {
            UpdateStatus(connectionKey, CacheStatus.Empty, CacheStatus.Loading);

            try
            {
                var cache = await _loader.LoadAsync(connectionKey, correlationId, cancellationToken).ConfigureAwait(false);
                _caches[connectionKey] = cache;
                UpdateStatus(connectionKey, CacheStatus.Loading, CacheStatus.Ready);
                _logger.Log(LogLevel.Info, "SchemaCache", $"Loaded fresh cache for {connectionKey}", correlationId: correlationId);
                return cache;
            }
            catch (Exception ex)
            {
                UpdateStatus(connectionKey, CacheStatus.Loading, CacheStatus.Empty);
                _logger.Log(LogLevel.Error, "SchemaCache", $"Failed to load fresh cache for {connectionKey}", ex, correlationId: correlationId);
                throw;
            }
        }

        private bool IsExpired(DatabaseCache cache)
        {
            return (DateTimeOffset.Now - cache.LoadedAt) > TimeToLive;
        }

        private void UpdateStatus(string connectionKey, CacheStatus oldStatus, CacheStatus newStatus)
        {
            StatusChanged?.Invoke(this, new CacheStatusChangedEventArgs(connectionKey, oldStatus, newStatus));
        }
    }
}
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
