using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Commands
{
    public sealed class RefreshSchemaCommand
    {
        public async Task ExecuteAsync(string connectionKey, CancellationToken cancellationToken = default)
        {
            var package = SqlEssentialsPackage.Instance;
            var logger = package?.Logger ?? NullLogger.Instance;
            logger.Log(LogLevel.Info, "Command", "RefreshSchemaCommand invoked", properties: new System.Collections.Generic.Dictionary<string, object>
            {
                { "connection_key", connectionKey ?? string.Empty }
            });

            if (package == null || package.SchemaCache == null)
            {
                logger.Log(LogLevel.Debug, "Command", "RefreshSchemaCommand aborted - package or cache unavailable");
                return;
            }

            try
            {
                await package.SchemaCache.RefreshAsync(connectionKey, null, cancellationToken).ConfigureAwait(false);
                logger.Log(LogLevel.Info, "Command", "RefreshSchemaCommand completed", properties: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "connection_key", connectionKey ?? string.Empty }
                });
            }
            catch (System.Exception ex)
            {
                logger.Log(LogLevel.Error, "Command", "RefreshSchemaCommand failed", ex, properties: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "connection_key", connectionKey ?? string.Empty }
                });
                throw;
            }
        }
    }
}
