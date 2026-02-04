using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public sealed class SmoMetadataLoader
    {
        public async Task<DatabaseCache> LoadAsync(string connectionKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tables = new List<TableDefinition>();
            var views = new List<TableDefinition>();
            var foreignKeys = new List<ForeignKeyRelationship>();
            var procedures = new List<ProcedureDefinition>();
            var functions = new List<FunctionDefinition>();

            await Task.Yield();

            return new DatabaseCache(
                connectionKey,
                System.DateTimeOffset.UtcNow,
                CacheStatus.Ready,
                tables,
                views,
                foreignKeys,
                procedures,
                functions);
        }
    }
}
