namespace SqlEssentials.Core.Context
{
    public interface IAliasBinding
    {
        string Alias { get; }
        string SchemaName { get; }
        string TableName { get; }
        string FullyQualifiedName { get; }
        int DefinedAtPosition { get; }
    }
}
