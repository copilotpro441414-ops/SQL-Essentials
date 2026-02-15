using System;
using System.Collections.Generic;
using System.Linq;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class DatabaseCacheTests
    {
        [Fact]
        public void Constructor_ThrowsForNullArguments()
        {
            var now = DateTimeOffset.UtcNow;
            var tables = new List<TableDefinition>();
            var views = new List<TableDefinition>();
            var foreignKeys = new List<ForeignKeyRelationship>();
            var procedures = new List<ProcedureDefinition>();
            var functions = new List<FunctionDefinition>();

            Assert.Throws<ArgumentNullException>(() => new DatabaseCache(null, now, CacheStatus.Ready, tables, views, foreignKeys, procedures, functions));
            Assert.Throws<ArgumentNullException>(() => new DatabaseCache("conn", now, CacheStatus.Ready, null, views, foreignKeys, procedures, functions));
            Assert.Throws<ArgumentNullException>(() => new DatabaseCache("conn", now, CacheStatus.Ready, tables, null, foreignKeys, procedures, functions));
            Assert.Throws<ArgumentNullException>(() => new DatabaseCache("conn", now, CacheStatus.Ready, tables, views, null, procedures, functions));
            Assert.Throws<ArgumentNullException>(() => new DatabaseCache("conn", now, CacheStatus.Ready, tables, views, foreignKeys, null, functions));
            Assert.Throws<ArgumentNullException>(() => new DatabaseCache("conn", now, CacheStatus.Ready, tables, views, foreignKeys, procedures, null));
        }

        [Fact]
        public void FindTable_ResolvesByNameAndFullyQualified_CaseInsensitive()
        {
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");

            Assert.NotNull(cache.FindTable("Users"));
            Assert.NotNull(cache.FindTable("users"));
            Assert.NotNull(cache.FindTable("[dbo].[Users]"));
            Assert.Null(cache.FindTable("Unknown"));
            Assert.Null(cache.FindTable(" "));
            Assert.Null(cache.FindTable(null));
        }

        [Fact]
        public void GetForeignKeysForTable_ReturnsParentAndReferencedMatches()
        {
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");

            var usersFks = cache.GetForeignKeysForTable("dbo", "Users");
            var ordersFks = cache.GetForeignKeysForTable("dbo", "Orders");

            Assert.Single(usersFks);
            Assert.Equal("FK_Orders_Users", usersFks[0].ConstraintName);

            Assert.Equal(2, ordersFks.Count);
            Assert.Contains(ordersFks, fk => fk.ConstraintName == "FK_Orders_Users");
            Assert.Contains(ordersFks, fk => fk.ConstraintName == "FK_OrderItems_Orders");
        }

        [Fact]
        public void GetForeignKeysForTable_InvalidInput_ReturnsEmpty()
        {
            var cache = TestMetadataBuilder.CreateSimpleCache("conn-A");

            Assert.Empty(cache.GetForeignKeysForTable(null, "Users"));
            Assert.Empty(cache.GetForeignKeysForTable("dbo", null));
            Assert.Empty(cache.GetForeignKeysForTable("", "Users"));
            Assert.Empty(cache.GetForeignKeysForTable("dbo", ""));
        }

        [Fact]
        public void InterfaceMembers_ExposeProjectedCollections()
        {
            IDatabaseCache cache = TestMetadataBuilder.CreateSimpleCache("conn-A");

            Assert.True(cache.Tables.Count > 0);
            Assert.True(cache.Views.Count >= 0);
            Assert.True(cache.ForeignKeys.Count > 0);
            Assert.True(cache.Procedures.Count >= 0);
            Assert.True(cache.Functions.Count >= 0);
            Assert.NotNull(cache.FindTable("Users"));
            Assert.True(cache.GetForeignKeysForTable("dbo", "Orders").Any());
        }
    }
}
