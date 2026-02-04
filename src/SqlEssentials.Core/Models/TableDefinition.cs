using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlEssentials.Core.Models
{
    public sealed class TableDefinition : ITableDefinition
    {
        private readonly IReadOnlyList<ColumnDefinition> _columns;

        public TableDefinition(string schemaName, string objectName, ObjectType type, IReadOnlyList<ColumnDefinition> columns)
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
            ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
            Type = type;
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            FullyQualifiedName = $"[{SchemaName}].[{ObjectName}]";
        }

        public string SchemaName { get; }
        public string ObjectName { get; }
        public string FullyQualifiedName { get; }
        public ObjectType Type { get; }
        public bool IsView => Type == ObjectType.View;

        public IReadOnlyList<ColumnDefinition> Columns => _columns;

        IReadOnlyList<IColumnDefinition> ITableDefinition.Columns => _columns;

        public ColumnDefinition GetColumn(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return _columns.FirstOrDefault(column =>
                column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        IColumnDefinition ITableDefinition.GetColumn(string name) => GetColumn(name);
    }

    public enum ObjectType
    {
        Table,
        View
    }
}
