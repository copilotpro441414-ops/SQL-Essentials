namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a parameter for a stored procedure or function.
    /// </summary>
    public interface IParameterDefinition
    {
        string Name { get; }
        string DataType { get; }
        ParameterDirection Direction { get; }
        string DefaultValue { get; }
        bool IsOutput { get; }
        bool HasDefault { get; }
    }
}
