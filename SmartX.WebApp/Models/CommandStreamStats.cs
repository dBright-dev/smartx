namespace SmartX.WebApp.Models
{
    /// <summary>
    /// Aggregate counters returned by GET /api/command-stream/stats.
    /// Mirrors SmartX.Client.Models.CommandStreamStats (Part 2).
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
