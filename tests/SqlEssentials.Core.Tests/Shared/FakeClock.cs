using System;

namespace SqlEssentials.Core.Tests.Shared
{
    /// <summary>
    /// Fake clock implementation for deterministic time-based testing.
    /// </summary>
    public sealed class FakeClock : SqlEssentials.Core.Metadata.IClock
    {
        private DateTimeOffset _currentTime;

        public FakeClock(DateTimeOffset? initialTime = null)
        {
            _currentTime = initialTime ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        }

        public DateTimeOffset UtcNow => _currentTime;

        /// <summary>
        /// Advances the clock by the specified duration.
        /// </summary>
        public void Advance(TimeSpan duration)
        {
            _currentTime = _currentTime.Add(duration);
        }

        /// <summary>
        /// Sets the clock to a specific time.
        /// </summary>
        public void SetTime(DateTimeOffset time)
        {
            _currentTime = time;
        }
    }
}
