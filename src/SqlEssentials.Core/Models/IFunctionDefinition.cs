using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a user-defined function in the database.
    /// </summary>
    public interface IFunctionDefinition
    {
        string SchemaName { get; }
        string FunctionName { get; }
        string FullyQualifiedName { get; }
        FunctionType Type { get; }
        string ReturnType { get; }
        bool IsTableValued { get; }
        IReadOnlyList<IParameterDefinition> Parameters { get; }
    }
}
