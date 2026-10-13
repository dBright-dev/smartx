using SmartX.API.Models;

namespace SmartX.API.DataStructures
{
    /// <summary>
    /// Priority queue where critical alerts bypass the standard FIFO stream.
    /// Lower numeric priority = dequeued first (Severity.Critical = 1 is top priority).
    /// REFERENCE: Microsoft, 2026. PriorityQueue&lt;TElement,TPriority&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2
    /// [Accessed 13 October 2026].
    /// </summary>
    public class AlertPriorityQueue
    {
        private readonly PriorityQueue<PriorityAlert, int> _pq = new();
        private readonly object _lock = new();

        public void Enqueue(PriorityAlert alert)
        {
            lock (_lock)
                _pq.Enqueue(alert, (int)alert.Severity);
        }

        public bool TryDequeue(out PriorityAlert? alert)
        {
            lock (_lock)
                return _pq.TryDequeue(out alert, out _);
        }

        /// <summary>Non-destructive view of the queue ordered by priority.</summary>
        public IEnumerable<PriorityAlert> Snapshot()
        {
            lock (_lock)
                return _pq.UnorderedItems
                    .OrderBy(i => i.Priority)
                    .ThenBy(i => i.Element.RaisedAtUtc)
                    .Select(i => i.Element)
                    .ToList();
        }

        public int Count { get { lock (_lock) return _pq.Count; } }
    } // (Microsoft, 2026)
}
