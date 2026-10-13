namespace SmartX.Client.Models
{
    /// <summary>
    /// Everything the Command Stream dashboard needs for a single render.
    /// </summary>
    public class CommandStreamViewModel
    {
        public CommandStreamStats Stats { get; set; } = new();
        public List<ManualCommand> History { get; set; } = new();
        public List<string> ActiveAlerts { get; set; } = new();
        public List<TimelineReading> Timeline { get; set; } = new();
        public List<SensorDevice> Devices { get; set; } = new();
    }
}
