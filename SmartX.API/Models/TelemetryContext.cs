using Microsoft.EntityFrameworkCore;

namespace SmartX.API.Models
{
    public class TelemetryContext : DbContext
    {
        public TelemetryContext(DbContextOptions<TelemetryContext> options) : base(options) { }

        // Exposes a DbSet per entity so EF can query and save instances (Microsoft, 2026c)
        public DbSet<SensorDevice> Devices => Set<SensorDevice>();
        public DbSet<SensorReading> Readings => Set<SensorReading>();
    }
}

// References:
//   Microsoft. (2026c). Tutorial: Create a Minimal API with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0