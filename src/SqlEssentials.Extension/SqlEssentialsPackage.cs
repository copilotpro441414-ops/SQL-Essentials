using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Extension.Logging;

namespace SqlEssentials.Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("SQL Essentials", "Smart T-SQL autocomplete", "0.1.0")]
    [ProvideAutoLoad("{ADFC4E6B-0397-11D1-9F4E-00A0C911004F}", PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SqlEssentialsPackage : AsyncPackage
    {
        internal static SqlEssentialsPackage Instance { get; private set; }

        internal ILogger Logger { get; private set; }
        internal ISchemaCache SchemaCache { get; private set; }
        internal IContextAnalyzer ContextAnalyzer { get; private set; }
        internal ICompletionEngine CompletionEngine { get; private set; }
        internal string CurrentConnectionKey { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            Instance = this;
            Logger = LoggerFactory.Create();

            Logger.Log(LogLevel.Info, "Package", "SQL Essentials Initialized", properties: new Dictionary<string, object>
            {
                { "version", "0.1.0" },
                { "pid", Process.GetCurrentProcess().Id }
            });

            SchemaCache = new SchemaCache(new SmoMetadataLoader(Logger), Logger);
            ContextAnalyzer = new ContextAnalyzer(Logger);
            CompletionEngine = new CompletionEngine(ContextAnalyzer, SchemaCache, Logger);

            await InitializeConnectionEventsAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (Logger as IDisposable)?.Dispose();
            }
            base.Dispose(disposing);
        }

        private Task InitializeConnectionEventsAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        internal async Task HandleConnectionChangedAsync(string connectionKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(connectionKey))
            {
                Logger.Log(LogLevel.Debug, "Package", "Connection cleared");
                return;
            }

            Logger.Log(LogLevel.Info, "Package", $"Connection changed to {connectionKey}");
            CurrentConnectionKey = connectionKey;

            try
            {
                await SchemaCache.GetOrLoadAsync(connectionKey, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Package", $"Failed to load metadata for connection {connectionKey}", ex);
            }
        }
    }
}
