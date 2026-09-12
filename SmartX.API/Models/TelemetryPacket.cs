namespace SmartX.API.Models
{
    public class TelemetryPacket<T>
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public T Value { get; set; } = default!;
        public string Unit { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
    }
}

// References:
//   Microsoft. (2026d). Create responses in Minimal API applications.
//     https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0