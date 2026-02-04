using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a foreign key constraint linking two tables.
    /// </summary>
    public interface IForeignKeyRelationship
    {
        string ConstraintName { get; }
        string ParentSchema { get; }
        string ParentTable { get; }
        string ParentFullName { get; }
        IReadOnlyList<string> ParentColumns { get; }
        string ReferencedSchema { get; }
        string ReferencedTable { get; }
        string ReferencedFullName { get; }
        IReadOnlyList<string> ReferencedColumns { get; }
    }
}
