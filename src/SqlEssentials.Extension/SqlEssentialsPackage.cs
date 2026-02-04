using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Metadata;

namespace SqlEssentials.Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("SQL Essentials", "Smart T-SQL autocomplete", "0.1.0")]
    [ProvideAutoLoad("{ADFC4E6B-0397-11D1-9F4E-00A0C911004F}", PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SqlEssentialsPackage : AsyncPackage
    {
        internal static SqlEssentialsPackage Instance { get; private set; }

        internal ISchemaCache SchemaCache { get; private set; }
        internal IContextAnalyzer ContextAnalyzer { get; private set; }
        internal ICompletionEngine CompletionEngine { get; private set; }
        internal string CurrentConnectionKey { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            Instance = this;
            SchemaCache = new SchemaCache(new SmoMetadataLoader());
            ContextAnalyzer = new ContextAnalyzer();
            CompletionEngine = new CompletionEngine(ContextAnalyzer, SchemaCache);

            await InitializeConnectionEventsAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task InitializeConnectionEventsAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        internal async Task HandleConnectionChangedAsync(string connectionKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(connectionKey))
            {
                return;
            }

            CurrentConnectionKey = connectionKey;
            await SchemaCache.GetOrLoadAsync(connectionKey, cancellationToken).ConfigureAwait(false);
        }
    }
}
