using System.Collections.Concurrent;

namespace SmartX.API.DataStructures
{
    /// <summary>
    /// FIFO queue for incoming telemetry packets.
    /// Uses <see cref="ConcurrentQueue{T}"/> so multiple device threads can
    /// enqueue without locking while a single consumer drains.
    /// REFERENCE: Microsoft, 2026. ConcurrentQueue&lt;T&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1
    /// [Accessed 13 October 2026].
    /// </summary>
    public class TelemetryQueue
    {
        private readonly ConcurrentQueue<object> _queue = new();

        public void Enqueue(object packet) => _queue.Enqueue(packet);

        public bool TryDequeue(out object? packet) => _queue.TryDequeue(out packet);

        public int Count => _queue.Count;

        public IEnumerable<object> Snapshot() => _queue.ToArray();
    } // (Microsoft, 2026)
}
