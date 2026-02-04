using System;
using System.Collections.Generic;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public interface IDatabaseCache
    {
        string ConnectionKey { get; }
        DateTimeOffset LastRefreshed { get; }
        CacheStatus Status { get; }

        IReadOnlyCollection<ITableDefinition> Tables { get; }
        IReadOnlyCollection<ITableDefinition> Views { get; }
        IReadOnlyCollection<IForeignKeyRelationship> ForeignKeys { get; }
        IReadOnlyCollection<IProcedureDefinition> Procedures { get; }
        IReadOnlyCollection<IFunctionDefinition> Functions { get; }

        ITableDefinition FindTable(string name);
        IReadOnlyList<IForeignKeyRelationship> GetForeignKeysForTable(string schemaName, string tableName);
    }
}
