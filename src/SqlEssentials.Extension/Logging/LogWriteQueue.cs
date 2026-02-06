using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Logging
{
    public sealed class LogWriteQueue : IDisposable
    {
        private readonly BlockingCollection<LogEntry> _queue;
        private readonly Action<LogEntry> _onWrite;
        private readonly CancellationTokenSource _cts;
        private readonly Task _workerTask;

        public LogWriteQueue(Action<LogEntry> onWrite)
        {
            _queue = new BlockingCollection<LogEntry>(LoggingConstants.MaxQueueSize);
            _onWrite = onWrite;
            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(ProcessQueue, _cts.Token);
        }

        public void Enqueue(LogEntry entry)
        {
            if (_queue.IsAddingCompleted) return;

            // Dropping if full to avoid blocking UI thread
            _queue.TryAdd(entry);
        }

        private void ProcessQueue()
        {
            try
            {
                foreach (var entry in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        _onWrite(entry);
                    }
                    catch
                    {
                        // Silently degrade if writing fails
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            try
            {
                _workerTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
            _queue.Dispose();
            _cts.Dispose();
        }
    }
}
