namespace SqlEssentials.Core.Formatting
{
    public interface ISqlFormatter
    {
        IFormattingResult Format(string sql, IFormattingProfile profile = null);

        IFormattingResult FormatSelection(string sql, int selectionStart, int selectionLength, IFormattingProfile profile = null);

        IFormattingProfile DefaultProfile { get; }
    }
}