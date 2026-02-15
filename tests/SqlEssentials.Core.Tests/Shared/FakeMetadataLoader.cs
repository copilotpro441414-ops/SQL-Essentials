using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Metadata;
using SqlEssentials.Core.Models;

namespace SqlEssentials.Core.Tests.Shared
{
    /// <summary>
    /// Fake metadata loader for deterministic testing without database dependencies.
    /// </summary>
    public sealed class FakeMetadataLoader
    {
        private readonly Func<string, string, CancellationToken, Task<DatabaseCache>> _loadFunc;

        public FakeMetadataLoader(Func<string, string, CancellationToken, Task<DatabaseCache>> loadFunc)
        {
            _loadFunc = loadFunc ?? throw new ArgumentNullException(nameof(loadFunc));
        }

        public Task<DatabaseCache> LoadAsync(string connectionKey, string correlationId = null, CancellationToken cancellationToken = default)
        {
            return _loadFunc(connectionKey, correlationId, cancellationToken);
        }

        /// <summary>
        /// Creates a simple fake loader that returns a predefined cache.
        /// </summary>
        public static FakeMetadataLoader CreateSimple(DatabaseCache cache)
        {
            return new FakeMetadataLoader((key, corId, ct) => Task.FromResult(cache));
        }

        /// <summary>
        /// Creates a fake loader that throws an exception.
        /// </summary>
        public static FakeMetadataLoader CreateFailing(Exception exception)
        {
            return new FakeMetadataLoader((key, corId, ct) => Task.FromException<DatabaseCache>(exception));
        }
    }
}
