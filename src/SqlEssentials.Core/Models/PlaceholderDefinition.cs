using SqlEssentials.Core.Snippets;

namespace SqlEssentials.Core.Models
{
    public sealed class PlaceholderDefinition : IPlaceholderDefinition
    {
        public PlaceholderDefinition(int index, string defaultValue = null, string description = null)
        {
            Index = index;
            DefaultValue = defaultValue;
            Description = description;
        }

        public int Index { get; }
        public string DefaultValue { get; }
        public string Description { get; }
    }
}