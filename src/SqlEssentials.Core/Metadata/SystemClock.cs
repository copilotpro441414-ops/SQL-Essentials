using System;

namespace SqlEssentials.Core.Metadata
{
    /// <summary>
    /// Production implementation of IClock that returns system time.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();

        private SystemClock()
        {
        }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
