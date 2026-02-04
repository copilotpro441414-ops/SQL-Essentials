using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Metadata;

namespace SqlEssentials.Extension.Completion
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("SqlEssentialsCompletion")]
    [ContentType("SQL")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    public sealed class SqlCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            var package = SqlEssentialsPackage.Instance;
            var engine = package?.CompletionEngine ?? CreateFallbackEngine();
            Func<string> connectionKeyProvider = () => package?.CurrentConnectionKey ?? string.Empty;

            return new SqlCompletionSource(engine, connectionKeyProvider);
        }

        private static ICompletionEngine CreateFallbackEngine()
        {
            var schemaCache = new SchemaCache(new SmoMetadataLoader());
            var contextAnalyzer = new ContextAnalyzer();
            return new CompletionEngine(contextAnalyzer, schemaCache);
        }
    }
}
