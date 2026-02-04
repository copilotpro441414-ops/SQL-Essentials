namespace SqlEssentials.Core.Models
{
    /// <summary>
    /// Represents a column in a table or view.
    /// </summary>
    public interface IColumnDefinition
    {
        string Name { get; }
        string DataType { get; }
        bool IsNullable { get; }
        bool IsPrimaryKey { get; }
        bool IsForeignKey { get; }
        bool IsIdentity { get; }
        bool IsComputed { get; }
        int? MaxLength { get; }
        int? Precision { get; }
        int? Scale { get; }
    }
}
