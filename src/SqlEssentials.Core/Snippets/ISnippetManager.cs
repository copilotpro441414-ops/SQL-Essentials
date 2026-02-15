using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlEssentials.Core.Snippets
{
    public interface ISnippetManager
    {
        IReadOnlyList<ISnippet> GetAllSnippets();
        IReadOnlyList<ISnippet> GetBuiltInSnippets();
        IReadOnlyList<ISnippet> GetCustomSnippets();
        ISnippet FindByShortcut(string shortcut);
        IReadOnlyList<ISnippet> FindByPrefix(string prefix);
        void AddCustomSnippet(ISnippet snippet);
        void UpdateCustomSnippet(string shortcut, ISnippet updatedSnippet);
        bool RemoveCustomSnippet(string shortcut);
        Task SaveAsync();
        Task ReloadAsync();
        string ExportToJson(IEnumerable<ISnippet> snippets = null);
        IReadOnlyList<ISnippet> ImportFromJson(string json);
    }
}