using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public class SmoMetadataLoader
    {
        private readonly ILogger _logger;

        public SmoMetadataLoader(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public virtual async Task<DatabaseCache> LoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            _logger.Log(LogLevel.Info, "SmoLoader", $"Loading metadata for {connectionKey}", correlationId: correlationId);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tables = new List<TableDefinition>();
                var views = new List<TableDefinition>();
                var foreignKeys = new List<ForeignKeyRelationship>();
                var procedures = new List<ProcedureDefinition>();
                var functions = new List<FunctionDefinition>();

                await Task.Yield();

                var cache = new DatabaseCache(
                    connectionKey,
                    DateTimeOffset.UtcNow,
                    CacheStatus.Ready,
                    tables,
                    views,
                    foreignKeys,
                    procedures,
                    functions);

                _logger.Log(LogLevel.Info, "SmoLoader", $"Successfully loaded metadata for {connectionKey}", 
                    properties: new Dictionary<string, object>
                    {
                        { "tables", tables.Count },
                        { "views", views.Count },
                        { "procs", procedures.Count },
                        { "funcs", functions.Count }
                    },
                    correlationId: correlationId);
                return cache;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "SmoLoader", $"Failed to load metadata for {connectionKey}", ex, correlationId: correlationId);
                throw;
            }
        }
    }
}
