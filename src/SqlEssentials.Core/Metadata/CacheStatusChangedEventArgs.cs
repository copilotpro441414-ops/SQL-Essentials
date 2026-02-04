using System;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Metadata
{
    public sealed class CacheStatusChangedEventArgs : EventArgs
    {
        public CacheStatusChangedEventArgs(string connectionKey, CacheStatus oldStatus, CacheStatus newStatus, Exception error = null)
        {
            ConnectionKey = connectionKey;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Error = error;
        }

        public string ConnectionKey { get; }
        public CacheStatus OldStatus { get; }
        public CacheStatus NewStatus { get; }
        public Exception Error { get; }
    }
}
