using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    public sealed class SchemaCache
    {
        public SchemaCache(
            string connectionKey,
            DateTimeOffset lastRefreshed,
            CacheStatus status,
            IReadOnlyDictionary<string, TableDefinition> tables,
            IReadOnlyDictionary<string, TableDefinition> views,
            IReadOnlyList<ForeignKeyRelationship> foreignKeys,
            IReadOnlyDictionary<string, ProcedureDefinition> procedures,
            IReadOnlyDictionary<string, FunctionDefinition> functions)
        {
            ConnectionKey = connectionKey ?? throw new ArgumentNullException(nameof(connectionKey));
            LastRefreshed = lastRefreshed;
            Status = status;
            Tables = tables ?? throw new ArgumentNullException(nameof(tables));
            Views = views ?? throw new ArgumentNullException(nameof(views));
            ForeignKeys = foreignKeys ?? throw new ArgumentNullException(nameof(foreignKeys));
            Procedures = procedures ?? throw new ArgumentNullException(nameof(procedures));
            Functions = functions ?? throw new ArgumentNullException(nameof(functions));
        }

        public string ConnectionKey { get; }
        public DateTimeOffset LastRefreshed { get; }
        public CacheStatus Status { get; }
        public IReadOnlyDictionary<string, TableDefinition> Tables { get; }
        public IReadOnlyDictionary<string, TableDefinition> Views { get; }
        public IReadOnlyList<ForeignKeyRelationship> ForeignKeys { get; }
        public IReadOnlyDictionary<string, ProcedureDefinition> Procedures { get; }
        public IReadOnlyDictionary<string, FunctionDefinition> Functions { get; }

        public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - LastRefreshed > ttl;
    }
}
