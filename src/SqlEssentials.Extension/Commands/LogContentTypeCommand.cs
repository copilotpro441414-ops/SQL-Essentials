using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Commands
{
    internal sealed class LogContentTypeCommand
    {
        private readonly AsyncPackage _package;

        private LogContentTypeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(CommandIds.CommandSet, CommandIds.LogContentType);
            var menuItem = new OleMenuCommand(Execute, cmdId);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                _ = new LogContentTypeCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var logger = SqlEssentialsPackage.Instance.Logger;
                try
                {
                    logger.Log(LogLevel.Debug, "Command", "LogContentTypeCommand triggered");
                    var contentType = await GetActiveContentTypeAsync().ConfigureAwait(true);
                    var message = $"Active content type: {contentType}";

                    logger.Log(LogLevel.Info, "Command", message);
                    await SetStatusBarTextAsync(message).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "Command", "Content type log failed", ex);
                }
            }).FileAndForget(nameof(LogContentTypeCommand));
        }

        private async Task<string> GetActiveContentTypeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var textManager = await _package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null)
            {
                return "unknown";
            }

            textManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var view);
            if (view == null)
            {
                return "unknown";
            }

            var componentModel = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
            {
                return "unknown";
            }

            var editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textView = editorAdapters.GetWpfTextView(view);
            var buffer = textView?.TextBuffer;

            return buffer?.ContentType?.TypeName ?? "unknown";
        }

        private async Task SetStatusBarTextAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var statusBar = await _package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar?.SetText(message);
        }
    }
}
