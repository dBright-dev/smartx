namespace SmartX.WebApp.Models
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}

// References:
//   Microsoft. (2026c). Tutorial: Create a Minimal API with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0