using System;
using System.ComponentModel.DataAnnotations;

namespace SmartX.API.Models
{
    public class SensorDevice
    {
        [Key]
        public string MacAddress { get; set; } = string.Empty;
        public string DeploymentLocation { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? ConfigurationFilePath { get; set; }
    }

    public enum SensorCategory
    {
        Environmental = 0,
        PowerConsumption = 1,
        Actuator = 2
    }
}
// References:
//   Microsoft. (2026c). Tutorial: Create a Minimal API with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0
//   Microsoft. (2026b). Create backend services for native mobile apps with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/mobile/native-mobile-backend?view=aspnetcore-10.0