using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public sealed class DatabaseCache : IDatabaseCache
    {
        private readonly Dictionary<string, TableDefinition> _tableIndex;
        private readonly IReadOnlyCollection<TableDefinition> _tables;
        private readonly IReadOnlyCollection<TableDefinition> _views;
        private readonly IReadOnlyCollection<ForeignKeyRelationship> _foreignKeys;
        private readonly IReadOnlyCollection<ProcedureDefinition> _procedures;
        private readonly IReadOnlyCollection<FunctionDefinition> _functions;

        public DatabaseCache(
            string connectionKey,
            DateTimeOffset lastRefreshed,
            CacheStatus status,
            IReadOnlyCollection<TableDefinition> tables,
            IReadOnlyCollection<TableDefinition> views,
            IReadOnlyCollection<ForeignKeyRelationship> foreignKeys,
            IReadOnlyCollection<ProcedureDefinition> procedures,
            IReadOnlyCollection<FunctionDefinition> functions)
        {
            ConnectionKey = connectionKey ?? throw new ArgumentNullException(nameof(connectionKey));
            LastRefreshed = lastRefreshed;
            Status = status;
            _tables = tables ?? throw new ArgumentNullException(nameof(tables));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _foreignKeys = foreignKeys ?? throw new ArgumentNullException(nameof(foreignKeys));
            _procedures = procedures ?? throw new ArgumentNullException(nameof(procedures));
            _functions = functions ?? throw new ArgumentNullException(nameof(functions));

            _tableIndex = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);
            IndexTables(tables);
            IndexTables(views);
        }

        public string ConnectionKey { get; }
        public DateTimeOffset LastRefreshed { get; }
        public CacheStatus Status { get; }

        public IReadOnlyCollection<TableDefinition> Tables => _tables;
        public IReadOnlyCollection<TableDefinition> Views => _views;
        public IReadOnlyCollection<ForeignKeyRelationship> ForeignKeys => _foreignKeys;
        public IReadOnlyCollection<ProcedureDefinition> Procedures => _procedures;
        public IReadOnlyCollection<FunctionDefinition> Functions => _functions;

        IReadOnlyCollection<ITableDefinition> IDatabaseCache.Tables => _tables;
        IReadOnlyCollection<ITableDefinition> IDatabaseCache.Views => _views;
        IReadOnlyCollection<IForeignKeyRelationship> IDatabaseCache.ForeignKeys => _foreignKeys;
        IReadOnlyCollection<IProcedureDefinition> IDatabaseCache.Procedures => _procedures;
        IReadOnlyCollection<IFunctionDefinition> IDatabaseCache.Functions => _functions;

        public TableDefinition FindTable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (_tableIndex.TryGetValue(name, out var table))
            {
                return table;
            }

            return null;
        }

        ITableDefinition IDatabaseCache.FindTable(string name) => FindTable(name);

        public IReadOnlyList<ForeignKeyRelationship> GetForeignKeysForTable(string schemaName, string tableName)
        {
            if (string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(tableName))
            {
                return Array.Empty<ForeignKeyRelationship>();
            }

            return _foreignKeys
                .Where(fk =>
                    (fk.ParentSchema.Equals(schemaName, StringComparison.OrdinalIgnoreCase) &&
                     fk.ParentTable.Equals(tableName, StringComparison.OrdinalIgnoreCase)) ||
                    (fk.ReferencedSchema.Equals(schemaName, StringComparison.OrdinalIgnoreCase) &&
                     fk.ReferencedTable.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        IReadOnlyList<IForeignKeyRelationship> IDatabaseCache.GetForeignKeysForTable(string schemaName, string tableName)
        {
            return GetForeignKeysForTable(schemaName, tableName).Cast<IForeignKeyRelationship>().ToList();
        }

        private void IndexTables(IEnumerable<TableDefinition> tables)
        {
            foreach (var table in tables)
            {
                _tableIndex[table.ObjectName] = table;
                _tableIndex[table.FullyQualifiedName] = table;
            }
        }
    }
}
