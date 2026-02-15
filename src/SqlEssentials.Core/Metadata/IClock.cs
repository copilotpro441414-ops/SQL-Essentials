using System;

namespace SqlEssentials.Core.Metadata
{
    /// <summary>
    /// Abstraction for retrieving the current time, enabling deterministic testing.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }
}
