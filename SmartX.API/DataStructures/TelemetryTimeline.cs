namespace SmartX.API.DataStructures
{
    public record TimelineReading(string MacAddress, DateTime Timestamp, double Value, string Unit);

    /// <summary>
    /// Sorted dictionary keyed by timestamp for chronological log display.
    /// Multiple readings at the same timestamp are stored in a list per key.
    /// REFERENCE: Microsoft, 2026. SortedDictionary&lt;TKey,TValue&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2
    /// [Accessed 13 October 2026].
    /// </summary>
    public class TelemetryTimeline
    {
        private readonly SortedDictionary<DateTime, List<TimelineReading>> _timeline = new();
        private readonly object _lock = new();

        public void Add(TimelineReading r)
        {
            lock (_lock)
            {
                if (!_timeline.TryGetValue(r.Timestamp, out var list))
                {
                    list = new List<TimelineReading>();
                    _timeline[r.Timestamp] = list;
                }
                list.Add(r);
            }
        }

        public IEnumerable<TimelineReading> Range(DateTime from, DateTime to)
        {
            lock (_lock)
            {
                return _timeline
                    .Where(kv => kv.Key >= from && kv.Key <= to)
                    .SelectMany(kv => kv.Value)
                    .ToList();
            }
        }

        public IEnumerable<TimelineReading> All()
        {
            lock (_lock)
                return _timeline.SelectMany(kv => kv.Value).ToList();
        }

        /// <summary>The N most recent readings, oldest first (for UI tables).</summary>
        public IEnumerable<TimelineReading> Latest(int n)
        {
            lock (_lock)
                return _timeline
                    .SelectMany(kv => kv.Value)
                    .OrderBy(r => r.Timestamp)
                    .TakeLast(n)
                    .ToList();
        }

        public int Count { get { lock (_lock) return _timeline.Values.Sum(v => v.Count); } }
    } // (Microsoft, 2026)
}
