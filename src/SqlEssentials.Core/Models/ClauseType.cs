namespace SqlEssentials.Core.Models
{
    public enum ClauseType
    {
        Unknown,
        Select,
        From,
        Where,
        Join,
        On,
        GroupBy,
        Having,
        OrderBy,
        Insert,
        Update,
        Delete,
        Set,
        Values
    }
}
