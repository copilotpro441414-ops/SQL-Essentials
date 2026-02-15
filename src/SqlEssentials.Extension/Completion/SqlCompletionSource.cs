using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Extension.Completion
{
    public sealed class SqlCompletionSource : IAsyncCompletionSource
    {
        private const string TypeInfoProperty = "SqlEssentials.TypeInfo";
        private const string DescriptionProperty = "SqlEssentials.Description";
        private const string SuggestionTypeProperty = "SqlEssentials.SuggestionType";

        private readonly ICompletionEngine _engine;
        private readonly ILogger _logger;
        private readonly Func<string> _connectionKeyProvider;

        public SqlCompletionSource(ICompletionEngine engine, ILogger logger, Func<string> connectionKeyProvider)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionKeyProvider = connectionKeyProvider ?? (() => string.Empty);
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Trace, "CompletionSource", $"InitializeCompletion trigger={trigger.Reason} char={trigger.Character}");

            if (trigger.Reason == CompletionTriggerReason.Invoke)
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, FindTokenSpan(triggerLocation));
            }

            if (trigger.Reason == CompletionTriggerReason.Insertion && trigger.Character == '.')
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, FindTokenSpan(triggerLocation));
            }

            if (trigger.Reason == CompletionTriggerReason.Insertion && trigger.Character == ' ')
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, FindTokenSpan(triggerLocation));
            }

            if (trigger.Reason == CompletionTriggerReason.Insertion && char.IsLetterOrDigit(trigger.Character))
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, FindTokenSpan(triggerLocation));
            }

            return CompletionStartData.DoesNotParticipateInCompletion;
        }

        public async Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid().ToString("N").Substring(0, 8);
            using (var scope = _logger.BeginScope("CompletionSource", "GetCompletionContextAsync", correlationId: correlationId))
            {
                var snapshot = triggerLocation.Snapshot;
                var queryText = snapshot.GetText();
                var connectionKey = _connectionKeyProvider();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var result = await _engine.GetCompletionsAsync(
                        queryText,
                        triggerLocation.Position,
                        connectionKey,
                        MapTrigger(trigger),
                        correlationId,
                        cancellationToken).ConfigureAwait(false);

                    var items = new List<CompletionItem>();
                    foreach (var suggestion in result.Suggestions)
                    {
                        var item = CreateCompletionItem(suggestion);
                        items.Add(item);
                    }

                    stopwatch.Stop();
                    _logger.Log(LogLevel.Info, "CompletionSource", "GetCompletionContextAsync complete", properties: new Dictionary<string, object>
                    {
                        { "item_count", items.Count },
                        { "trigger", trigger.Reason.ToString() },
                        { "connection_key", connectionKey ?? string.Empty },
                        { "fallback_items", 0 },
                        { "elapsed_ms", stopwatch.Elapsed.TotalMilliseconds }
                    }, correlationId: correlationId);

                    return new CompletionContext(items.ToImmutableArray());
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.Log(LogLevel.Error, "CompletionSource", "GetCompletionContextAsync failed", ex, properties: new Dictionary<string, object>
                    {
                        { "connection_key", connectionKey ?? string.Empty },
                        { "elapsed_ms", stopwatch.Elapsed.TotalMilliseconds }
                    }, correlationId: correlationId);
                    throw;
                }
            }
        }

        public Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem item,
            CancellationToken cancellationToken)
        {
            var tooltip = BuildTooltip(item);
            return Task.FromResult<object>(tooltip);
        }

        private CompletionItem CreateCompletionItem(ISuggestion suggestion)
        {
            // Create the completion item with display text
            var item = new CompletionItem(
                displayText: suggestion.DisplayText,
                source: this,
                icon: default,
                filters: ImmutableArray<CompletionFilter>.Empty,
                suffix: string.Empty,
                insertText: suggestion.InsertionText,
                sortText: suggestion.SortText ?? suggestion.DisplayText,
                filterText: suggestion.FilterText ?? suggestion.DisplayText,
                automationText: suggestion.DisplayText,
                attributeIcons: ImmutableArray<ImageElement>.Empty);

            // Store additional data for tooltip
            item.Properties.AddProperty(TypeInfoProperty, suggestion.TypeInfo ?? string.Empty);
            item.Properties.AddProperty(DescriptionProperty, suggestion.Description ?? string.Empty);
            item.Properties.AddProperty(SuggestionTypeProperty, suggestion.Type.ToString());

            return item;
        }

        private static string BuildTooltip(CompletionItem item)
        {
            var parts = new List<string>();

            if (item.Properties.TryGetProperty(SuggestionTypeProperty, out string suggestionType) &&
                !string.IsNullOrEmpty(suggestionType))
            {
                parts.Add($"({suggestionType})");
            }

            if (item.Properties.TryGetProperty(TypeInfoProperty, out string typeInfo) &&
                !string.IsNullOrEmpty(typeInfo))
            {
                parts.Add($"Type: {typeInfo}");
            }

            if (item.Properties.TryGetProperty(DescriptionProperty, out string description) &&
                !string.IsNullOrEmpty(description))
            {
                parts.Add(description);
            }

            if (parts.Count == 0)
            {
                return null;
            }

            return string.Join("\n", parts);
        }

        private static SnapshotSpan FindTokenSpan(SnapshotPoint point)
        {
            var snapshot = point.Snapshot;
            var position = point.Position;
            var start = position;
            var end = position;

            while (start > 0)
            {
                var ch = snapshot[start - 1];
                if (!IsTokenChar(ch))
                {
                    break;
                }

                start--;
            }

            while (end < snapshot.Length)
            {
                var ch = snapshot[end];
                if (!IsTokenChar(ch))
                {
                    break;
                }

                end++;
            }

            return new SnapshotSpan(snapshot, start, end - start);
        }

        private static bool IsTokenChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '[' || ch == ']';
        }

        private static TriggerReason MapTrigger(CompletionTrigger trigger)
        {
            if (trigger.Reason == CompletionTriggerReason.Invoke)
            {
                return TriggerReason.Explicit;
            }

            if (trigger.Reason == CompletionTriggerReason.Insertion && trigger.Character == '.')
            {
                return TriggerReason.DotAfterIdentifier;
            }

            if (trigger.Reason == CompletionTriggerReason.Insertion && trigger.Character == ' ')
            {
                return TriggerReason.SpaceAfterKeyword;
            }

            return TriggerReason.Typing;
        }
    }
}
