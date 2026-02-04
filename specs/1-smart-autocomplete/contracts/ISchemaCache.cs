// Contract: ISchemaCache
// Purpose: Schema metadata caching and retrieval
// Source: FR-012 to FR-015 (Metadata Management)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqlEssentials.Core.Contracts
{
    /// <summary>
    /// Provides cached access to database schema metadata.
    /// Implementations must be thread-safe and support async operations.
    /// </summary>
    public interface ISchemaCache
    {
        /// <summary>
        /// Gets the current cache status for a connection.
        /// </summary>
        CacheStatus GetStatus(string connectionKey);

        /// <summary>
        /// Gets or loads the schema cache for a database connection.
        /// Returns cached data if available and not expired; otherwise triggers background refresh.
        /// </summary>
        /// <param name="connectionKey">Unique key in format "server:database"</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The database cache, or null if not yet loaded</returns>
        Task<IDatabaseCache?> GetOrLoadAsync(string connectionKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces a refresh of the cache for a connection.
        /// FR-014: Manual refresh command for schema cache.
        /// </summary>
        Task RefreshAsync(string connectionKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all cached data for a connection.
        /// </summary>
        void Invalidate(string connectionKey);

        /// <summary>
        /// Clears all cached data for all connections.
        /// </summary>
        void InvalidateAll();

        /// <summary>
        /// Gets or sets the cache TTL. Default: 30 minutes.
        /// FR-015: Configurable cache TTL.
        /// </summary>
        TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Raised when cache status changes (Loading, Ready, Error).
        /// </summary>
        event EventHandler<CacheStatusChangedEventArgs>? StatusChanged;
    }

    /// <summary>
    /// Read-only view of cached database metadata.
    /// </summary>
    public interface IDatabaseCache
    {
        string ConnectionKey { get; }
        DateTimeOffset LastRefreshed { get; }
        CacheStatus Status { get; }

        IReadOnlyCollection<ITableDefinition> Tables { get; }
        IReadOnlyCollection<ITableDefinition> Views { get; }
        IReadOnlyCollection<IForeignKeyRelationship> ForeignKeys { get; }
        IReadOnlyCollection<IProcedureDefinition> Procedures { get; }
        IReadOnlyCollection<IFunctionDefinition> Functions { get; }

        /// <summary>
        /// Finds a table or view by name (case-insensitive).
        /// Supports both "[Schema].[Name]" and "Name" formats.
        /// </summary>
        ITableDefinition? FindTable(string name);

        /// <summary>
        /// Gets foreign keys where the specified table is either parent or referenced.
        /// </summary>
        IReadOnlyList<IForeignKeyRelationship> GetForeignKeysForTable(string schemaName, string tableName);
    }

    public interface ITableDefinition
    {
        string SchemaName { get; }
        string ObjectName { get; }
        string FullyQualifiedName { get; }
        bool IsView { get; }
        IReadOnlyList<IColumnDefinition> Columns { get; }
    }

    public interface IColumnDefinition
    {
        string Name { get; }
        string DataType { get; }
        bool IsNullable { get; }
        bool IsPrimaryKey { get; }
        bool IsForeignKey { get; }
    }

    public interface IForeignKeyRelationship
    {
        string ConstraintName { get; }
        string ParentSchema { get; }
        string ParentTable { get; }
        IReadOnlyList<string> ParentColumns { get; }
        string ReferencedSchema { get; }
        string ReferencedTable { get; }
        IReadOnlyList<string> ReferencedColumns { get; }
    }

    public interface IProcedureDefinition
    {
        string SchemaName { get; }
        string ProcedureName { get; }
        string FullyQualifiedName { get; }
        IReadOnlyList<IParameterDefinition> Parameters { get; }
    }

    public interface IFunctionDefinition
    {
        string SchemaName { get; }
        string FunctionName { get; }
        string FullyQualifiedName { get; }
        string ReturnType { get; }
        IReadOnlyList<IParameterDefinition> Parameters { get; }
    }

    public interface IParameterDefinition
    {
        string Name { get; }
        string DataType { get; }
        bool IsOutput { get; }
    }

    public enum CacheStatus
    {
        Empty,
        Loading,
        Ready,
        Refreshing,
        Error
    }

    public sealed class CacheStatusChangedEventArgs : EventArgs
    {
        public string ConnectionKey { get; }
        public CacheStatus OldStatus { get; }
        public CacheStatus NewStatus { get; }
        public Exception? Error { get; }

        public CacheStatusChangedEventArgs(string connectionKey, CacheStatus oldStatus, CacheStatus newStatus, Exception? error = null)
        {
            ConnectionKey = connectionKey;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Error = error;
        }
    }
}
