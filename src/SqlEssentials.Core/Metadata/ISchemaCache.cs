using System;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public interface ISchemaCache
    {
        CacheStatus GetStatus(string connectionKey);

        Task<IDatabaseCache> GetOrLoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default);

        Task RefreshAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default);

        void Invalidate(string connectionKey);

        void InvalidateAll();

        TimeSpan TimeToLive { get; set; }

        event EventHandler<CacheStatusChangedEventArgs> StatusChanged;
    }
}
