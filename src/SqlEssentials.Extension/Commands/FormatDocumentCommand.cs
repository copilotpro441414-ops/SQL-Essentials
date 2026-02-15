using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Commands
{
    internal sealed class FormatDocumentCommand
    {
        private readonly AsyncPackage _package;

        private FormatDocumentCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(CommandIds.CommandSet, CommandIds.FormatDocument);
            var menuItem = new OleMenuCommand(Execute, cmdId);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                _ = new FormatDocumentCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var host = SqlEssentialsPackage.Instance;
                var logger = host?.Logger ?? NullLogger.Instance;
                var formatter = host?.SqlFormatter;

                if (formatter == null)
                {
                    logger.Log(LogLevel.Warning, "Command", "FormatDocumentCommand aborted - formatter unavailable");
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var textView = await GetActiveTextViewAsync().ConfigureAwait(true);
                if (textView == null)
                {
                    logger.Log(LogLevel.Debug, "Command", "FormatDocumentCommand aborted - no active SQL text view");
                    return;
                }

                var snapshot = textView.TextSnapshot;
                var source = snapshot.GetText();

                logger.Log(LogLevel.Info, "Command", "FormatDocumentCommand invoked", properties: new Dictionary<string, object>
                {
                    { "selection_start", 0 },
                    { "selection_length", snapshot.Length },
                    { "profile", formatter.DefaultProfile?.Name ?? "Default" }
                });

                var result = formatter.Format(source);
                if (!result.Success)
                {
                    logger.Log(LogLevel.Warning, "Command", "FormatDocumentCommand encountered formatting errors", properties: new Dictionary<string, object>
                    {
                        { "error_count", result.Errors.Count }
                    });
                    return;
                }

                if (string.Equals(source, result.FormattedText, StringComparison.Ordinal))
                {
                    return;
                }

                using (var edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(0, snapshot.Length, result.FormattedText);
                    edit.Apply();
                }
            }).FileAndForget(nameof(FormatDocumentCommand));
        }

        private async Task<IWpfTextView> GetActiveTextViewAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var textManager = await _package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null)
            {
                return null;
            }

            textManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var view);
            if (view == null)
            {
                return null;
            }

            var componentModel = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
            {
                return null;
            }

            var editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            return editorAdapters?.GetWpfTextView(view);
        }
    }
}
