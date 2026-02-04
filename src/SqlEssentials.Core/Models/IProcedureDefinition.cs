using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a stored procedure in the database.
    /// </summary>
    public interface IProcedureDefinition
    {
        string SchemaName { get; }
        string ProcedureName { get; }
        string FullyQualifiedName { get; }
        IReadOnlyList<IParameterDefinition> Parameters { get; }
    }
}
