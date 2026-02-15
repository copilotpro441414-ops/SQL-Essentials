namespace SqlEssentials.Core.Context
{
    public interface IJoinContext
    {
        string LeftTableOrAlias { get; }
        string RightTableOrAlias { get; }
        string ResolvedLeftTable { get; }
        string ResolvedRightTable { get; }
    }
}