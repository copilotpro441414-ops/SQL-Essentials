namespace SqlEssentials.Core.Models
{
    public sealed class PlaceholderSpan
    {
        public PlaceholderSpan(int placeholderIndex, int startPosition, int length, string currentText)
        {
            PlaceholderIndex = placeholderIndex;
            StartPosition = startPosition;
            Length = length;
            CurrentText = currentText ?? string.Empty;
        }

        public int PlaceholderIndex { get; }
        public int StartPosition { get; }
        public int Length { get; }
        public string CurrentText { get; }
    }
}