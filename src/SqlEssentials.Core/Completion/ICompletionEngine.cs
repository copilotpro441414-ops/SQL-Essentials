using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Completion
{
    public interface ICompletionEngine
    {
        Task<ICompletionResult> GetCompletionsAsync(
            string queryText,
            int cursorPosition,
            string connectionKey,
            TriggerReason trigger,
            CancellationToken cancellationToken = default);
    }
}
