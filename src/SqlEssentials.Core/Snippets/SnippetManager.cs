using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Snippets
{
    public sealed class SnippetManager : ISnippetManager
    {
        private const string BuiltInResourceSuffix = "Snippets.Resources.BuiltInSnippets.json";

        private readonly ILogger _logger;
        private readonly string _customSnippetsPath;
        private readonly Dictionary<string, ISnippet> _builtInSnippets;
        private readonly Dictionary<string, ISnippet> _customSnippets;

        public SnippetManager(ILogger logger = null, string customSnippetsPath = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _customSnippetsPath = string.IsNullOrWhiteSpace(customSnippetsPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SqlEssentials", "snippets.json")
                : customSnippetsPath;

            _builtInSnippets = new Dictionary<string, ISnippet>(StringComparer.OrdinalIgnoreCase);
            _customSnippets = new Dictionary<string, ISnippet>(StringComparer.OrdinalIgnoreCase);

            foreach (var snippet in LoadBuiltInSnippets())
            {
                _builtInSnippets[snippet.Shortcut] = snippet;
            }

            _logger.Log(LogLevel.Debug, "SnippetManager", "Built-in snippets loaded", properties: new Dictionary<string, object>
            {
                { "snippet_count", _builtInSnippets.Count }
            });
        }

        public IReadOnlyList<ISnippet> GetAllSnippets()
        {
            return _builtInSnippets.Values
                .Concat(_customSnippets.Values)
                .OrderBy(s => s.Shortcut, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<ISnippet> GetBuiltInSnippets()
        {
            return _builtInSnippets.Values.OrderBy(s => s.Shortcut, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<ISnippet> GetCustomSnippets()
        {
            return _customSnippets.Values.OrderBy(s => s.Shortcut, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public ISnippet FindByShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                return null;
            }

            if (_customSnippets.TryGetValue(shortcut, out var custom))
            {
                return custom;
            }

            return _builtInSnippets.TryGetValue(shortcut, out var builtIn) ? builtIn : null;
        }

        public IReadOnlyList<ISnippet> FindByPrefix(string prefix)
        {
            var candidates = GetAllSnippets();
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return candidates;
            }

            return candidates
                .Where(s => s.Shortcut.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void AddCustomSnippet(ISnippet snippet)
        {
            ValidateSnippet(snippet);

            if (_builtInSnippets.ContainsKey(snippet.Shortcut) || _customSnippets.ContainsKey(snippet.Shortcut))
            {
                throw new InvalidOperationException($"Snippet shortcut '{snippet.Shortcut}' already exists.");
            }

            var customSnippet = new Snippet(
                snippet.Shortcut,
                snippet.Name,
                snippet.Description,
                snippet.Category,
                snippet.BodyLines,
                snippet.Placeholders,
                isBuiltIn: false);

            _customSnippets[customSnippet.Shortcut] = customSnippet;
            _logger.Log(LogLevel.Debug, "SnippetManager", "Custom snippet added", properties: new Dictionary<string, object>
            {
                { "shortcut", customSnippet.Shortcut },
                { "custom_count", _customSnippets.Count }
            });
        }

        public void UpdateCustomSnippet(string shortcut, ISnippet updatedSnippet)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                throw new ArgumentException("Shortcut is required.", nameof(shortcut));
            }

            ValidateSnippet(updatedSnippet);

            if (!_customSnippets.ContainsKey(shortcut))
            {
                throw new KeyNotFoundException($"Custom snippet '{shortcut}' was not found.");
            }

            if (!shortcut.Equals(updatedSnippet.Shortcut, StringComparison.OrdinalIgnoreCase) &&
                (_builtInSnippets.ContainsKey(updatedSnippet.Shortcut) || _customSnippets.ContainsKey(updatedSnippet.Shortcut)))
            {
                throw new InvalidOperationException($"Snippet shortcut '{updatedSnippet.Shortcut}' already exists.");
            }

            _customSnippets.Remove(shortcut);
            _customSnippets[updatedSnippet.Shortcut] = new Snippet(
                updatedSnippet.Shortcut,
                updatedSnippet.Name,
                updatedSnippet.Description,
                updatedSnippet.Category,
                updatedSnippet.BodyLines,
                updatedSnippet.Placeholders,
                isBuiltIn: false);

            _logger.Log(LogLevel.Debug, "SnippetManager", "Custom snippet updated", properties: new Dictionary<string, object>
            {
                { "shortcut", updatedSnippet.Shortcut },
                { "custom_count", _customSnippets.Count }
            });
        }

        public bool RemoveCustomSnippet(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                return false;
            }

            var removed = _customSnippets.Remove(shortcut);
            if (removed)
            {
                _logger.Log(LogLevel.Debug, "SnippetManager", "Custom snippet removed", properties: new Dictionary<string, object>
                {
                    { "shortcut", shortcut },
                    { "custom_count", _customSnippets.Count }
                });
            }

            return removed;
        }

        public async Task SaveAsync()
        {
            var dir = Path.GetDirectoryName(_customSnippetsPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = ExportToJson(_customSnippets.Values);
            using (var writer = new StreamWriter(_customSnippetsPath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
            }
        }

        public async Task ReloadAsync()
        {
            _customSnippets.Clear();
            if (!File.Exists(_customSnippetsPath))
            {
                return;
            }

            string json;
            using (var reader = new StreamReader(_customSnippetsPath, Encoding.UTF8))
            {
                json = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            foreach (var snippet in ImportFromJson(json))
            {
                _customSnippets[snippet.Shortcut] = new Snippet(
                    snippet.Shortcut,
                    snippet.Name,
                    snippet.Description,
                    snippet.Category,
                    snippet.BodyLines,
                    snippet.Placeholders,
                    isBuiltIn: false);
            }
        }

        public string ExportToJson(IEnumerable<ISnippet> snippets = null)
        {
            var source = (snippets ?? _customSnippets.Values).ToList();
            var payload = new SnippetCatalog
            {
                Snippets = source.Select(ToDto).ToList()
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        public IReadOnlyList<ISnippet> ImportFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<ISnippet>();
            }

            var catalog = JsonConvert.DeserializeObject<SnippetCatalog>(json);
            if (catalog?.Snippets == null)
            {
                return Array.Empty<ISnippet>();
            }

            return catalog.Snippets.Select(dto => ToSnippet(dto, isBuiltIn: false)).Cast<ISnippet>().ToList();
        }

        private static void ValidateSnippet(ISnippet snippet)
        {
            if (snippet == null)
            {
                throw new ArgumentNullException(nameof(snippet));
            }

            if (string.IsNullOrWhiteSpace(snippet.Shortcut))
            {
                throw new ArgumentException("Snippet shortcut cannot be empty.", nameof(snippet));
            }

            if (snippet.BodyLines == null || snippet.BodyLines.Count == 0)
            {
                throw new ArgumentException("Snippet body must contain at least one line.", nameof(snippet));
            }
        }

        private IReadOnlyList<Snippet> LoadBuiltInSnippets()
        {
            var assembly = typeof(SnippetManager).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(BuiltInResourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new InvalidOperationException($"Embedded resource '{BuiltInResourceSuffix}' was not found.");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Snippet resource stream unavailable."), Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                var catalog = JsonConvert.DeserializeObject<SnippetCatalog>(json);
                if (catalog?.Snippets == null)
                {
                    return Array.Empty<Snippet>();
                }

                return catalog.Snippets.Select(dto => ToSnippet(dto, isBuiltIn: true)).ToList();
            }
        }

        private static Snippet ToSnippet(SnippetDto dto, bool isBuiltIn)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var placeholders = new Dictionary<int, IPlaceholderDefinition>();
            if (dto.Placeholders != null)
            {
                foreach (var kvp in dto.Placeholders)
                {
                    if (!int.TryParse(kvp.Key, out var index))
                    {
                        throw new FormatException($"Invalid placeholder index '{kvp.Key}'.");
                    }

                    placeholders[index] = new PlaceholderDefinition(index, kvp.Value?.Default, kvp.Value?.Description);
                }
            }

            return new Snippet(
                dto.Shortcut,
                dto.Name,
                dto.Description,
                dto.Category,
                dto.Body ?? new[] { string.Empty },
                placeholders,
                isBuiltIn);
        }

        private static SnippetDto ToDto(ISnippet snippet)
        {
            return new SnippetDto
            {
                Shortcut = snippet.Shortcut,
                Name = snippet.Name,
                Description = snippet.Description,
                Category = snippet.Category,
                Body = snippet.BodyLines?.ToArray() ?? Array.Empty<string>(),
                Placeholders = (snippet.Placeholders ?? new Dictionary<int, IPlaceholderDefinition>())
                    .ToDictionary(
                        p => p.Key.ToString(),
                        p => new PlaceholderDto
                        {
                            Default = p.Value.DefaultValue,
                            Description = p.Value.Description
                        })
            };
        }

        private sealed class SnippetCatalog
        {
            [JsonProperty("snippets")]
            public List<SnippetDto> Snippets { get; set; }
        }

        private sealed class SnippetDto
        {
            [JsonProperty("shortcut")]
            public string Shortcut { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("body")]
            public string[] Body { get; set; }

            [JsonProperty("placeholders")]
            public Dictionary<string, PlaceholderDto> Placeholders { get; set; }
        }

        private sealed class PlaceholderDto
        {
            [JsonProperty("default")]
            public string Default { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }
    }
}