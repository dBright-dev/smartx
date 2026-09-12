namespace SmartX.API.Models
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double Wattage { get; set; }
        // Overload the + operator
        public static SensorReading operator +(SensorReading a, SensorReading b)
        {
            return new SensorReading { Wattage = a.Wattage + b.Wattage };
        }
    }
}

// References:
//   Microsoft. (2026c). Tutorial: Create a Minimal API with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0