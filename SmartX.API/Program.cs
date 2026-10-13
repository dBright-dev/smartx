
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SmartX.API.Models;
using System.Security.Cryptography;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register the DbContext in the DI container (Microsoft, 2026c).
        // The string "SmartX" is the database name; because it's in-memory it resets
        // on every app restart, as explained in the Minimal API tutorial.
        builder.Services.AddDbContext<TelemetryContext>(opt =>
            opt.UseInMemoryDatabase("SmartX"));

        // Enable OpenAPI (Microsoft, 2026c).
        builder.Services.AddOpenApi();

        // CORS so the MAUI app can call the API from another origin (Microsoft, 2026b).
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        var app = builder.Build();

        //if (app.Environment.IsDevelopment())
        //{
        //    app.MapOpenApi();
        //}
        
        app.UseCors();
        app.UseHttpsRedirection();

        // -------- Health check endpoint --------
        // Lightweight liveness probe. Returns 200 OK when the API can reach its DbContext.
        // REFERENCE: Microsoft, 2026. Health checks in ASP.NET Core.
        // [online] Available at:
        // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
        // [Accessed 12 September 2026].
        app.MapGet("/health", async (TelemetryContext db) =>
        {
            try
            {
                // Cheap DB round-trip proves the in-memory store is responsive.
                _ = await db.Devices.CountAsync();
                return TypedResults.Ok(new
                {
                    status = "Healthy",
                    timestampUtc = DateTime.UtcNow,
                    database = "Connected"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Health check failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        // Endpoint groups. Using MapGroup reduces repetitive URL prefixes and lets us
        // customise the whole group in one place (Microsoft, 2026c).
        var devices = app.MapGroup("/api/devices");
        var telemetry = app.MapGroup("/api/telemetry");

        // -------- Device registration --------
        // Returns TypedResults.Created so OpenAPI metadata is auto-generated
        // (Microsoft, 2026d).
        devices.MapPost("/register", async (SensorDevice device, TelemetryContext db) =>
        {
            device.RegisteredAt = DateTime.UtcNow;
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/devices/{device.MacAddress}", device);
        });

        // -------- Get all devices --------
        devices.MapGet("/", async (TelemetryContext db) =>
            await db.Devices.ToListAsync());

        // -------- Get one device by MAC --------
        // Uses Results<Ok<T>, NotFound> union so the return type is fully declared
        // (Microsoft, 2026d).
        devices.MapGet("/{macAddress}", async Task<Results<Ok<SensorDevice>, NotFound>>
            (string macAddress, TelemetryContext db) =>
        {
            return await db.Devices.FindAsync(macAddress)
                is SensorDevice device
                    ? TypedResults.Ok(device)
                    : TypedResults.NotFound();
        });

        // -------- Get readings for a device --------
        devices.MapGet("/{macAddress}/readings", async (string macAddress, TelemetryContext db) =>
        {
            var readings = await db.Readings
                .Where(r => r.Unit != null)   // example filter
                .ToListAsync();
            return TypedResults.Ok(readings);
        });

        // -------- File upload (multipart) --------
        devices.MapPost("/{macAddress}/upload", async (string macAddress, IFormFile file, IWebHostEnvironment env) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file supplied.");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, $"{macAddress}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // TODO: Update the SensorProfile in your dictionary with this file path.
            return Results.Ok(new { message = "File uploaded successfully", path = filePath });
        })
        .DisableAntiforgery();

        // -------- Telemetry ingestion (typed endpoints) --------
        // One endpoint per value type keeps the generic wrapper closed at runtime
        // (Microsoft, 2026c). Each returns 200 OK via TypedResults.Ok (Microsoft, 2026d).
        telemetry.MapPost("/float", async (TelemetryPacket<float> packet, TelemetryContext db) =>
        {
            await StoreReadingAsync(packet.DeviceId, packet.Value, packet.Unit, db);
            return TypedResults.Ok(new { status = "received", value = packet.Value });
        });

        telemetry.MapPost("/int", async (TelemetryPacket<int> packet, TelemetryContext db) =>
        {
            await StoreReadingAsync(packet.DeviceId, packet.Value, packet.Unit, db);
            return TypedResults.Ok(new { status = "received", value = packet.Value });
        });

        telemetry.MapPost("/bool", async (TelemetryPacket<bool> packet, TelemetryContext db) =>
        {
            // bool is stored as 1.0 / 0.0 for uniformity
            await StoreReadingAsync(packet.DeviceId, packet.Value ? 1.0 : 0.0, packet.Unit, db);
            return TypedResults.Ok(new { status = "received", value = packet.Value });
        });

        app.Run();

        // Local helper that writes a reading and updates the device's state.
        static async Task StoreReadingAsync(string deviceId, double value, string unit, TelemetryContext db)
        {
            db.Readings.Add(new SensorReading
            {
                Timestamp = DateTime.UtcNow,
                Value = value,
                Unit = unit
            });
            await db.SaveChangesAsync();
        }
    }
}

// References:
//   Microsoft. (2026c). Tutorial: Create a Minimal API with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0
//   Microsoft. (2026d). Create responses in Minimal API applications.
//     https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0
