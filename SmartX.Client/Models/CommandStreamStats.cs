namespace SmartX.Client.Models
{
    /// <summary>
    /// Aggregate counters returned by GET /api/command-stream/stats.
    /// One object drives every dashboard card so a single round-trip is enough.
    /// </summary>
    public class CommandStreamStats
    {
        public int QueueSize { get; set; }
        public int PriorityAlertCount { get; set; }
        public int CommandHistorySize { get; set; }
        public int DeviceCount { get; set; }
        public int TimelineReadingCount { get; set; }
        public int ActiveAlertCount { get; set; }
        public long SearchEvents { get; set; }

        public bool ApiReachable { get; set; } = true;
    }
}
