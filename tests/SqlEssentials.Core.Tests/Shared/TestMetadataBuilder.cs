using System;
using System.Collections.Generic;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Tests.Shared
{
    /// <summary>
    /// Builder for creating deterministic test metadata fixtures.
    /// </summary>
    public sealed class TestMetadataBuilder
    {
        private readonly List<TableDefinition> _tables = new List<TableDefinition>();
        private readonly List<TableDefinition> _views = new List<TableDefinition>();
        private readonly List<ForeignKeyRelationship> _foreignKeys = new List<ForeignKeyRelationship>();
        private readonly List<ProcedureDefinition> _procedures = new List<ProcedureDefinition>();
        private readonly List<FunctionDefinition> _functions = new List<FunctionDefinition>();

        /// <summary>
        /// Adds a simple table with columns.
        /// </summary>
        public TestMetadataBuilder AddTable(string schemaName, string tableName, params string[] columnNames)
        {
            var columns = new List<ColumnDefinition>();
            foreach (var columnName in columnNames)
            {
                columns.Add(new ColumnDefinition(columnName, "nvarchar", false, false, false, false, false, null, null, null));
            }

            _tables.Add(new TableDefinition(schemaName, tableName, ObjectType.Table, columns));
            return this;
        }

        /// <summary>
        /// Adds a table with detailed column specifications.
        /// </summary>
        public TestMetadataBuilder AddTable(string schemaName, string tableName, IReadOnlyList<ColumnDefinition> columns)
        {
            _tables.Add(new TableDefinition(schemaName, tableName, ObjectType.Table, columns));
            return this;
        }

        /// <summary>
        /// Adds a simple view with columns.
        /// </summary>
        public TestMetadataBuilder AddView(string schemaName, string viewName, params string[] columnNames)
        {
            var columns = new List<ColumnDefinition>();
            foreach (var columnName in columnNames)
            {
                columns.Add(new ColumnDefinition(columnName, "nvarchar", false, false, false, false, false, null, null, null));
            }

            _views.Add(new TableDefinition(schemaName, viewName, ObjectType.View, columns));
            return this;
        }

        /// <summary>
        /// Adds a foreign key relationship.
        /// </summary>
        public TestMetadataBuilder AddForeignKey(
            string name,
            string parentSchema,
            string parentTable,
            string parentColumn,
            string referencedSchema,
            string referencedTable,
            string referencedColumn)
        {
            _foreignKeys.Add(new ForeignKeyRelationship(
                name,
                parentSchema,
                parentTable,
                new[] { parentColumn },
                referencedSchema,
                referencedTable,
                new[] { referencedColumn }));
            return this;
        }

        /// <summary>
        /// Adds a stored procedure.
        /// </summary>
        public TestMetadataBuilder AddProcedure(string schemaName, string procedureName, IReadOnlyList<ParameterDefinition> parameters = null)
        {
            _procedures.Add(new ProcedureDefinition(schemaName, procedureName, parameters ?? Array.Empty<ParameterDefinition>()));
            return this;
        }

        /// <summary>
        /// Adds a function.
        /// </summary>
        public TestMetadataBuilder AddFunction(string schemaName, string functionName, string returnType = "int", IReadOnlyList<ParameterDefinition> parameters = null)
        {
            _functions.Add(new FunctionDefinition(schemaName, functionName, FunctionType.Scalar, returnType, parameters ?? Array.Empty<ParameterDefinition>()));
            return this;
        }

        /// <summary>
        /// Builds a DatabaseCache with the configured metadata.
        /// </summary>
        public DatabaseCache Build(string connectionKey = "test-connection", DateTimeOffset? lastRefreshed = null, CacheStatus status = CacheStatus.Ready)
        {
            return new DatabaseCache(
                connectionKey,
                lastRefreshed ?? DateTimeOffset.UtcNow,
                status,
                _tables,
                _views,
                _foreignKeys,
                _procedures,
                _functions);
        }

        /// <summary>
        /// Creates a simple database cache with common test tables.
        /// </summary>
        public static DatabaseCache CreateSimpleCache(string connectionKey = "test-connection")
        {
            return new TestMetadataBuilder()
                .AddTable("dbo", "Users", "UserId", "Username", "Email")
                .AddTable("dbo", "Orders", "OrderId", "UserId", "OrderDate", "TotalAmount")
                .AddTable("dbo", "OrderItems", "OrderItemId", "OrderId", "ProductId", "Quantity")
                .AddForeignKey("FK_Orders_Users", "dbo", "Orders", "UserId", "dbo", "Users", "UserId")
                .AddForeignKey("FK_OrderItems_Orders", "dbo", "OrderItems", "OrderId", "dbo", "Orders", "OrderId")
                .Build(connectionKey);
        }
    }
}
