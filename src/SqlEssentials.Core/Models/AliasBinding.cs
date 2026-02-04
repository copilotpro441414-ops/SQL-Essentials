using System;
using SqlEssentials.Core.Context;

namespace SqlEssentials.Core.Models
{
    public sealed class AliasBinding : IAliasBinding
    {
        public AliasBinding(string alias, string schemaName, string tableName, int definedAtPosition)
        {
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            DefinedAtPosition = definedAtPosition;
            FullyQualifiedName = $"[{SchemaName}].[{TableName}]";
        }

        public string Alias { get; }
        public string SchemaName { get; }
        public string TableName { get; }
        public string FullyQualifiedName { get; }
        public int DefinedAtPosition { get; }
    }
}
