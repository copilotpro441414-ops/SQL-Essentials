using System;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class MetadataSupportTypesTests
    {
        [Fact]
        public void CacheStatusChangedEventArgs_AssignsAllProperties()
        {
            var error = new InvalidOperationException("boom");

            var args = new CacheStatusChangedEventArgs("conn-A", CacheStatus.Loading, CacheStatus.Ready, error);

            Assert.Equal("conn-A", args.ConnectionKey);
            Assert.Equal(CacheStatus.Loading, args.OldStatus);
            Assert.Equal(CacheStatus.Ready, args.NewStatus);
            Assert.Same(error, args.Error);
        }

        [Fact]
        public void CacheStatusChangedEventArgs_ErrorIsOptional()
        {
            var args = new CacheStatusChangedEventArgs("conn-A", CacheStatus.Empty, CacheStatus.Loading);

            Assert.Null(args.Error);
        }

        [Fact]
        public void SystemClock_Instance_ReturnsUtcNow()
        {
            var before = DateTimeOffset.UtcNow;
            var now = SystemClock.Instance.UtcNow;
            var after = DateTimeOffset.UtcNow;

            Assert.True(now >= before.AddSeconds(-1));
            Assert.True(now <= after.AddSeconds(1));
        }
    }
}
