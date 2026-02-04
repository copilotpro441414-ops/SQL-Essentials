using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a table or view in the database.
    /// </summary>
    public interface ITableDefinition
    {
        string SchemaName { get; }
        string ObjectName { get; }
        string FullyQualifiedName { get; }
        bool IsView { get; }
        IReadOnlyList<IColumnDefinition> Columns { get; }

        /// <summary>
        /// Finds a column by name (case-insensitive).
        /// </summary>
        IColumnDefinition GetColumn(string name);
    }
}
