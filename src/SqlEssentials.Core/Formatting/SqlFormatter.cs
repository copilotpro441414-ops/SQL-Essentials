using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Formatting
{
    public sealed class SqlFormatter : ISqlFormatter
    {
        private const string DefaultProfileResourceSuffix = "Formatting.Resources.DefaultProfile.json";

        private readonly ILogger _logger;

        public SqlFormatter(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            DefaultProfile = LoadDefaultProfile();
        }

        public IFormattingProfile DefaultProfile { get; }

        public IFormattingResult Format(string sql, IFormattingProfile profile = null)
        {
            return FormatCore(sql ?? string.Empty, profile ?? DefaultProfile);
        }

        public IFormattingResult FormatSelection(string sql, int selectionStart, int selectionLength, IFormattingProfile profile = null)
        {
            var source = sql ?? string.Empty;
            if (selectionStart < 0 || selectionLength < 0 || selectionStart > source.Length || selectionStart + selectionLength > source.Length)
            {
                return FormattingResult.Failure(source, new[]
                {
                    new FormattingError(0, 0, "Selection range is outside the SQL text.", isWarning: false)
                });
            }

            if (selectionLength == 0)
            {
                return Format(source, profile);
            }

            var selectedSql = source.Substring(selectionStart, selectionLength);
            return FormatCore(selectedSql, profile ?? DefaultProfile);
        }

        private IFormattingResult FormatCore(string sql, IFormattingProfile profile)
        {
            using (_logger.BeginScope("Formatter", "format_sql", properties: new Dictionary<string, object>
            {
                { "input_length", sql.Length },
                { "profile", profile?.Name ?? "Default" }
            }))
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var parser = new TSql160Parser(initialQuotedIdentifiers: false);
                    IList<ParseError> errors;
                    TSqlFragment fragment;

                    using (var reader = new StringReader(sql))
                    {
                        fragment = parser.Parse(reader, out errors);
                    }

                    if (errors != null && errors.Count > 0)
                    {
                        return FormattingResult.Failure(sql, errors.Select(e => new FormattingError(e.Line, e.Column, e.Message, isWarning: false)));
                    }

                    var visitor = new FormattingVisitor(profile);
                    fragment.Accept(visitor);
                    var formatted = visitor.Apply(sql);

                    _logger.Log(LogLevel.Debug, "Formatter", "SQL formatting complete", properties: new Dictionary<string, object>
                    {
                        { "token_count", visitor.TokenCount },
                        { "formatted_length", formatted.Length },
                        { "elapsed_ms", stopwatch.ElapsedMilliseconds }
                    });

                    return FormattingResult.SuccessResult(formatted);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Formatter", "SQL formatting failed", ex);
                    return FormattingResult.Failure(sql, new[]
                    {
                        new FormattingError(0, 0, ex.Message, isWarning: false)
                    });
                }
            }
        }

        private static IFormattingProfile LoadDefaultProfile()
        {
            var assembly = typeof(SqlFormatter).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(DefaultProfileResourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return CreateFallbackProfile();
            }

            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Formatting profile resource stream unavailable.")))
                {
                    var json = reader.ReadToEnd();
                    var dto = JsonConvert.DeserializeObject<FormattingProfileDto>(json);
                    if (dto == null)
                    {
                        return CreateFallbackProfile();
                    }

                    return new FormattingProfile(
                        dto.Name,
                        dto.KeywordCase,
                        dto.IndentStyle,
                        dto.IndentSize,
                        dto.CommaPosition,
                        dto.JoinOnNewLine,
                        dto.IndentOnPredicate,
                        dto.SelectColumnsOnSeparateLines,
                        dto.MaxLineLength,
                        dto.BlankLineBeforeClauses,
                        dto.AlignEquals);
                }
            }
            catch
            {
                return CreateFallbackProfile();
            }
        }

        private static IFormattingProfile CreateFallbackProfile()
        {
            return new FormattingProfile(
                name: "Default",
                keywordCase: KeywordCase.Upper,
                indentStyle: IndentStyle.Spaces,
                indentSize: 4,
                commaPosition: CommaPosition.Trailing,
                joinOnNewLine: true,
                indentOnPredicate: true,
                selectColumnsOnSeparateLines: false,
                maxLineLength: 120,
                blankLineBeforeClauses: false,
                alignEquals: false);
        }

        private sealed class FormattingProfileDto
        {
            public string Name { get; set; }
            public KeywordCase KeywordCase { get; set; }
            public IndentStyle IndentStyle { get; set; }
            public int IndentSize { get; set; }
            public CommaPosition CommaPosition { get; set; }
            public bool JoinOnNewLine { get; set; }
            public bool IndentOnPredicate { get; set; }
            public bool SelectColumnsOnSeparateLines { get; set; }
            public int MaxLineLength { get; set; }
            public bool BlankLineBeforeClauses { get; set; }
            public bool AlignEquals { get; set; }
        }
    }
}