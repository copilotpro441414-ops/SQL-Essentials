using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Completion;
using SqlEssentials.Core.Context;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Completion
{
    public sealed class JoinPredicateGeneratorTests
    {
        [Fact]
        public void Generate_ThrowsForNullArguments()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");
            var joinContext = new JoinContext("u", "o", "Users", "Orders");

            Assert.Throws<ArgumentNullException>(() => generator.Generate(null, cache));
            Assert.Throws<ArgumentNullException>(() => generator.Generate(joinContext, null));
        }

        [Fact]
        public void Generate_ReturnsForeignKeySuggestion_ForMatchingTables()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");
            var joinContext = new JoinContext("u", "o", "Users", "Orders");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.NotEmpty(suggestions);
            Assert.Contains(suggestions, s => s.MatchType == JoinMatchType.ForeignKey);
            Assert.Contains(suggestions, s => s.InsertionText == "u.UserId = o.UserId");
        }

        [Fact]
        public void Generate_ReturnsForeignKeySuggestion_WhenForeignKeyDirectionMatchesLeftToRight()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");
            var joinContext = new JoinContext("o", "u", "Orders", "Users");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.NotEmpty(suggestions);
            Assert.Contains(suggestions, s => s.MatchType == JoinMatchType.ForeignKey);
            Assert.Contains(suggestions, s => s.InsertionText == "o.UserId = u.UserId");
        }

        [Fact]
        public void Generate_ReturnsColumnNameFallback_WhenNoForeignKeyExists()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithoutForeignKeys();
            var joinContext = new JoinContext("u", "p", "Users", "Profiles");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.NotEmpty(suggestions);
            Assert.DoesNotContain(suggestions, s => s.MatchType == JoinMatchType.ForeignKey);
            Assert.Contains(suggestions, s => s.MatchType == JoinMatchType.ColumnNameExact && s.InsertionText == "u.UserId = p.UserId");
        }

        [Fact]
        public void Generate_ReturnsSimilarFallback_WhenNamesDifferByUnderscore()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithSimilarColumnsWithoutForeignKeys();
            var joinContext = new JoinContext("u", "p", "Users", "Profiles");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.Contains(suggestions, s => s.MatchType == JoinMatchType.ColumnNameSimilar && s.InsertionText == "u.User_Id = p.UserId");
        }

        [Fact]
        public void Generate_RanksForeignKeySuggestions_HigherThanFallback()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithForeignKeyAndFallback();
            var joinContext = new JoinContext("u", "o", "Users", "Orders");

            var suggestions = generator.Generate(joinContext, cache).ToList();

            Assert.True(suggestions.Count >= 1);
            var first = suggestions[0];
            Assert.Equal(JoinMatchType.ForeignKey, first.MatchType);
        }

        [Fact]
        public void Generate_DeduplicatesIdenticalPredicates_FromFkAndFallback()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithForeignKeyAndFallback();
            var joinContext = new JoinContext("u", "o", "Users", "Orders");

            var suggestions = generator.Generate(joinContext, cache)
                .Where(s => s.InsertionText == "u.UserId = o.UserId")
                .ToList();

            Assert.Single(suggestions);
        }

        [Fact]
        public void Generate_ReturnsMultiColumnPredicate_ForCompositeForeignKey()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithCompositeForeignKey();
            var joinContext = new JoinContext("h", "l", "OrderHeader", "OrderLine");

            var suggestions = generator.Generate(joinContext, cache);

            var predicate = suggestions.Single(s => s.MatchType == JoinMatchType.ForeignKey).InsertionText;
            Assert.Equal("h.OrderId = l.OrderId AND h.TenantId = l.TenantId", predicate);
        }

        [Fact]
        public void Generate_ReturnsEmpty_WhenTablesCannotBeResolved()
        {
            var logger = new RecordingLogger(LogLevel.Debug);
            var generator = new JoinPredicateGenerator(logger);
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");
            var joinContext = new JoinContext("x", "y", "MissingLeft", "MissingRight");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.Empty(suggestions);
            Assert.Contains(logger.Entries, e => e.Component == "JoinPredicateGenerator");
        }

        [Fact]
        public void Generate_UsesTableOrAliasFallback_WhenResolvedNamesDoNotExist()
        {
            var generator = new JoinPredicateGenerator(NullLogger.Instance);
            var cache = BuildCacheWithoutForeignKeys();
            var joinContext = new JoinContext("Users", "Profiles", "MissingLeft", "MissingRight");

            var suggestions = generator.Generate(joinContext, cache);

            Assert.NotEmpty(suggestions);
            Assert.Contains(suggestions, s => s.InsertionText == "Users.UserId = Profiles.UserId");
        }

        private static DatabaseCache BuildCacheWithoutForeignKeys()
        {
            var users = new TableDefinition("dbo", "Users", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("UserId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("Username", "nvarchar", false, false, false, false, false, null, null, null)
            });

            var profiles = new TableDefinition("dbo", "Profiles", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("ProfileId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("UserId", "int", false, false, false, false, false, null, null, null)
            });

            return new DatabaseCache(
                "conn-A",
                DateTimeOffset.UtcNow,
                CacheStatus.Ready,
                new[] { users, profiles },
                Array.Empty<TableDefinition>(),
                Array.Empty<ForeignKeyRelationship>(),
                Array.Empty<ProcedureDefinition>(),
                Array.Empty<FunctionDefinition>());
        }

        private static DatabaseCache BuildCacheWithForeignKeyAndFallback()
        {
            var users = new TableDefinition("dbo", "Users", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("UserId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("Username", "nvarchar", false, false, false, false, false, null, null, null)
            });

            var orders = new TableDefinition("dbo", "Orders", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("OrderId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("UserId", "int", false, false, true, false, false, null, null, null)
            });

            var fk = new ForeignKeyRelationship(
                "FK_Orders_Users",
                "dbo",
                "Orders",
                new[] { "UserId" },
                "dbo",
                "Users",
                new[] { "UserId" });

            return new DatabaseCache(
                "conn-A",
                DateTimeOffset.UtcNow,
                CacheStatus.Ready,
                new[] { users, orders },
                Array.Empty<TableDefinition>(),
                new[] { fk },
                Array.Empty<ProcedureDefinition>(),
                Array.Empty<FunctionDefinition>());
        }

        private static DatabaseCache BuildCacheWithCompositeForeignKey()
        {
            var header = new TableDefinition("dbo", "OrderHeader", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("OrderId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("TenantId", "int", false, true, false, false, false, null, null, null)
            });

            var line = new TableDefinition("dbo", "OrderLine", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("LineId", "int", false, true, false, false, false, null, null, null),
                new ColumnDefinition("OrderId", "int", false, false, true, false, false, null, null, null),
                new ColumnDefinition("TenantId", "int", false, false, true, false, false, null, null, null)
            });

            var compositeFk = new ForeignKeyRelationship(
                "FK_OrderLine_OrderHeader",
                "dbo",
                "OrderLine",
                new[] { "OrderId", "TenantId" },
                "dbo",
                "OrderHeader",
                new[] { "OrderId", "TenantId" });

            return new DatabaseCache(
                "conn-A",
                DateTimeOffset.UtcNow,
                CacheStatus.Ready,
                new[] { header, line },
                Array.Empty<TableDefinition>(),
                new[] { compositeFk },
                Array.Empty<ProcedureDefinition>(),
                Array.Empty<FunctionDefinition>());
        }

        private static DatabaseCache BuildCacheWithSimilarColumnsWithoutForeignKeys()
        {
            var users = new TableDefinition("dbo", "Users", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("User_Id", "int", false, false, false, false, false, null, null, null)
            });

            var profiles = new TableDefinition("dbo", "Profiles", ObjectType.Table, new List<ColumnDefinition>
            {
                new ColumnDefinition("UserId", "int", false, false, false, false, false, null, null, null)
            });

            return new DatabaseCache(
                "conn-A",
                DateTimeOffset.UtcNow,
                CacheStatus.Ready,
                new[] { users, profiles },
                Array.Empty<TableDefinition>(),
                Array.Empty<ForeignKeyRelationship>(),
                Array.Empty<ProcedureDefinition>(),
                Array.Empty<FunctionDefinition>());
        }
    }
}