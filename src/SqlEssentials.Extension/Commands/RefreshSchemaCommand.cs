using System.Threading;
using System.Threading.Tasks;

namespace SqlEssentials.Extension.Commands
{
    public sealed class RefreshSchemaCommand
    {
        public async Task ExecuteAsync(string connectionKey, CancellationToken cancellationToken = default)
        {
            var package = SqlEssentialsPackage.Instance;
            if (package == null || package.SchemaCache == null)
            {
                return;
            }

            await package.SchemaCache.RefreshAsync(connectionKey, null, cancellationToken).ConfigureAwait(false);
        }
    }
}
