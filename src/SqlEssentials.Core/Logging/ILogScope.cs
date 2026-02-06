using System;

namespace SqlEssentials.Core.Logging
{
    public interface ILogScope : IDisposable
    {
        string CorrelationId { get; }
        string Component { get; }
        string Operation { get; }
        TimeSpan Elapsed { get; }
    }
}
