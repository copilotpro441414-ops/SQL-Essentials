using System;

namespace SqlEssentials.Core.Models
{
    public sealed class DatabaseConnection
    {
        public DatabaseConnection(string serverName, string databaseName, AuthenticationMethod authMethod, bool isConnected)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException("Server name is required.", nameof(serverName));
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name is required.", nameof(databaseName));
            }

            ServerName = serverName;
            DatabaseName = databaseName;
            AuthMethod = authMethod;
            IsConnected = isConnected;
            ConnectionKey = $"{serverName}:{databaseName}";
        }

        public string ServerName { get; }
        public string DatabaseName { get; }
        public string ConnectionKey { get; }
        public AuthenticationMethod AuthMethod { get; }
        public bool IsConnected { get; }
    }
}
