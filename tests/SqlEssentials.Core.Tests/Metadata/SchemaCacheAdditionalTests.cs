using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;
using SqlEssentials.Core.Tests.Shared;
using Xunit;

namespace SqlEssentials.Core.Tests.Metadata
{
    public sealed class SchemaCacheAdditionalTests
    {
        [Fact]
        public void Constructor_ThrowsForNullLoader()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlEssentials.Core.Metadata.SchemaCache(null));
        }

        [Fact]
        public void GetStatus_NullConnectionKey_ReturnsEmpty()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());

            Assert.Equal(CacheStatus.Empty, cache.GetStatus(null));
        }

        [Fact]
        public async Task GetOrLoadAsync_NullConnectionKey_Throws()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());

            await Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetOrLoadAsync(null));
        }

        [Fact]
        public async Task RefreshAsync_NullConnectionKey_Throws()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());

            await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RefreshAsync(null));
        }

        [Fact]
        public void Invalidate_NullConnectionKey_DoesNothing()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());

            cache.Invalidate(null);

            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-A"));
        }

        [Fact]
        public async Task InvalidateAll_ClearsAllEntries()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());

            await cache.GetOrLoadAsync("conn-A");
            await cache.GetOrLoadAsync("conn-B");
            Assert.Equal(CacheStatus.Ready, cache.GetStatus("conn-A"));
            Assert.Equal(CacheStatus.Ready, cache.GetStatus("conn-B"));

            cache.InvalidateAll();

            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-A"));
            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-B"));
        }

        [Fact]
        public async Task LoadFresh_RaisesStatusChanged_EmptyToLoadingToReady()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new SuccessLoader("conn-A"), NullLogger.Instance, new FakeClock());
            var events = new List<CacheStatusChangedEventArgs>();
            cache.StatusChanged += (_, e) => events.Add(e);

            await cache.GetOrLoadAsync("conn-A");

            Assert.Equal(2, events.Count);
            Assert.Equal(CacheStatus.Empty, events[0].OldStatus);
            Assert.Equal(CacheStatus.Loading, events[0].NewStatus);
            Assert.Equal(CacheStatus.Loading, events[1].OldStatus);
            Assert.Equal(CacheStatus.Ready, events[1].NewStatus);
        }

        [Fact]
        public async Task RefreshAsync_ExistingEntry_RaisesRefreshingAndReady()
        {
            var loader = new SuccessLoader("conn-A");
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(loader, NullLogger.Instance, new FakeClock());
            var events = new List<CacheStatusChangedEventArgs>();
            cache.StatusChanged += (_, e) => events.Add(e);

            await cache.GetOrLoadAsync("conn-A");
            events.Clear();

            await cache.RefreshAsync("conn-A");

            Assert.Equal(2, events.Count);
            Assert.Equal(CacheStatus.Ready, events[0].OldStatus);
            Assert.Equal(CacheStatus.Refreshing, events[0].NewStatus);
            Assert.Equal(CacheStatus.Refreshing, events[1].OldStatus);
            Assert.Equal(CacheStatus.Ready, events[1].NewStatus);
        }

        [Fact]
        public async Task LoadFresh_Failure_ThrowsAndResetsToEmpty()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new ThrowingLoader(), NullLogger.Instance, new FakeClock());
            var events = new List<CacheStatusChangedEventArgs>();
            cache.StatusChanged += (_, e) => events.Add(e);

            await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrLoadAsync("conn-A"));

            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-A"));
            Assert.Equal(2, events.Count);
            Assert.Equal(CacheStatus.Empty, events[0].OldStatus);
            Assert.Equal(CacheStatus.Loading, events[0].NewStatus);
            Assert.Equal(CacheStatus.Loading, events[1].OldStatus);
            Assert.Equal(CacheStatus.Empty, events[1].NewStatus);
        }

        [Fact]
        public async Task RefreshAsync_NoExistingEntry_Failure_DoesNotThrowAndStaysEmpty()
        {
            var cache = new SqlEssentials.Core.Metadata.SchemaCache(new ThrowingLoader(), NullLogger.Instance, new FakeClock());
            var events = new List<CacheStatusChangedEventArgs>();
            cache.StatusChanged += (_, e) => events.Add(e);

            await cache.RefreshAsync("conn-A");

            Assert.Equal(CacheStatus.Empty, cache.GetStatus("conn-A"));
            Assert.Single(events);
            Assert.Equal(CacheStatus.Refreshing, events[0].OldStatus);
            Assert.Equal(CacheStatus.Empty, events[0].NewStatus);
        }

        private sealed class SuccessLoader : SmoMetadataLoader
        {
            private readonly string _connectionKey;

            public SuccessLoader(string connectionKey)
            {
                _connectionKey = connectionKey;
            }

            public override Task<DatabaseCache> LoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
            {
                var key = string.IsNullOrWhiteSpace(connectionKey) ? _connectionKey : connectionKey;
                return Task.FromResult(TestMetadataBuilder.CreateSimpleCache(key));
            }
        }

        private sealed class ThrowingLoader : SmoMetadataLoader
        {
            public override Task<DatabaseCache> LoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Load failed");
            }
        }
    }
}
