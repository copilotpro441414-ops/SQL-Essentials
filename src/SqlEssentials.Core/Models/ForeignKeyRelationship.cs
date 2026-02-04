using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    public sealed class ForeignKeyRelationship : IForeignKeyRelationship
    {
        public ForeignKeyRelationship(
            string constraintName,
            string parentSchema,
            string parentTable,
            IReadOnlyList<string> parentColumns,
            string referencedSchema,
            string referencedTable,
            IReadOnlyList<string> referencedColumns)
        {
            ConstraintName = constraintName ?? throw new ArgumentNullException(nameof(constraintName));
            ParentSchema = parentSchema ?? throw new ArgumentNullException(nameof(parentSchema));
            ParentTable = parentTable ?? throw new ArgumentNullException(nameof(parentTable));
            ParentColumns = parentColumns ?? throw new ArgumentNullException(nameof(parentColumns));
            ReferencedSchema = referencedSchema ?? throw new ArgumentNullException(nameof(referencedSchema));
            ReferencedTable = referencedTable ?? throw new ArgumentNullException(nameof(referencedTable));
            ReferencedColumns = referencedColumns ?? throw new ArgumentNullException(nameof(referencedColumns));
        }

        public string ConstraintName { get; }
        public string ParentSchema { get; }
        public string ParentTable { get; }
        public IReadOnlyList<string> ParentColumns { get; }
        public string ReferencedSchema { get; }
        public string ReferencedTable { get; }
        public IReadOnlyList<string> ReferencedColumns { get; }

        public string ParentFullName => $"[{ParentSchema}].[{ParentTable}]";
        public string ReferencedFullName => $"[{ReferencedSchema}].[{ReferencedTable}]";
    }
}
