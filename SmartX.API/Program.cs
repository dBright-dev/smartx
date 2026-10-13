
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SmartX.API.DataStructures;
using SmartX.API.Models;
using System.Security.Cryptography;
using System.Text.Json;

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

        // ---- Part 2: in-memory data structures ----
        // Registered as singletons so every request shares ONE instance —
        // the queue/stack/dictionary state survives across requests.
        // (Microsoft, 2026. Dependency injection in ASP.NET Core.)
        builder.Services.AddSingleton<TelemetryQueue>();           // Queue<T>/ConcurrentQueue<T>  (FIFO)
        builder.Services.AddSingleton<AlertPriorityQueue>();       // PriorityQueue<TElement,TPriority>
        builder.Services.AddSingleton<CommandHistory>();           // Stack<T>                     (undo)
        builder.Services.AddSingleton<DeviceRegistry>();           // Dictionary<TKey,TValue>      (O(1) lookup)
        builder.Services.AddSingleton<TelemetryTimeline>();        // SortedDictionary<TKey,TValue>
        builder.Services.AddSingleton<ActiveAlertsSet>();          // HashSet<T>                   (dedup)
        builder.Services.AddSingleton<SearchPatternTracker>();     // Frequency map + sorting      (predictive engine)

        var app = builder.Build();

        // ------------------------------------------------------------
        // Part 2 — seed the in-memory data structures with demo data so
        // the Command Stream dashboard is populated on first run.
        // ------------------------------------------------------------
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TelemetryContext>();
            var registry = scope.ServiceProvider.GetRequiredService<DeviceRegistry>();
            var timeline = scope.ServiceProvider.GetRequiredService<TelemetryTimeline>();
            var tracker = scope.ServiceProvider.GetRequiredService<SearchPatternTracker>();

            // 1) Devices: guarantee the demo fleet exists, then mirror the EF
            //    store into the O(1) dictionary registry.
            var demoDevices = new[]
            {
                new SensorDevice { MacAddress = "AA:BB:CC:00:00:01", DeploymentLocation = "North Field", Zone = "Zone A", NodeId = "NODE-01", Category = SensorCategory.Environmental },
                new SensorDevice { MacAddress = "AA:BB:CC:00:00:02", DeploymentLocation = "Pump House",  Zone = "Zone A", NodeId = "NODE-02", Category = SensorCategory.Actuator },
                new SensorDevice { MacAddress = "AA:BB:CC:00:00:03", DeploymentLocation = "Boiler Room", Zone = "Zone B", NodeId = "NODE-03", Category = SensorCategory.PowerConsumption },
                new SensorDevice { MacAddress = "AA:BB:CC:00:00:04", DeploymentLocation = "South Field", Zone = "Zone C", NodeId = "NODE-04", Category = SensorCategory.Environmental }
            };

            foreach (var d in demoDevices)
                if (!db.Devices.Any(x => x.MacAddress == d.MacAddress))
                    db.Devices.Add(d);
            db.SaveChanges();

            foreach (var d in db.Devices.ToList())
                registry.Register(d);

            // 2) Sorted timeline: a few chronological readings for the last hour.
            if (timeline.Count == 0)
            {
                var now = DateTime.UtcNow;
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:01", now.AddMinutes(-50), 41.7, "%"));
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:03", now.AddMinutes(-40), 2204, "W"));
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:02", now.AddMinutes(-30), 3.2, "bar"));
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:04", now.AddMinutes(-20), 58.9, "%"));
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:01", now.AddMinutes(-10), 40.2, "%"));
                timeline.Add(new TimelineReading("AA:BB:CC:00:00:03", now.AddMinutes(-5), 2310, "W"));
            }

            // 3) Predictive engine: seed co-occurrence history so the very first
            //    dashboard visit already returns meaningful suggestions.
            if (tracker.EventCount == 0)
            {
                var t0 = DateTime.UtcNow.AddHours(-1);
                var seeded = new (string User, string Term, int Minutes)[]
                {
                    ("op1", "Moisture Sensor B", 0),
                    ("op1", "Pump A",            1),
                    ("op1", "Moisture Sensor B", 6),
                    ("op1", "Valve 3",           7),
                    ("op1", "Moisture Sensor B", 12),
                    ("op1", "Pump A",           13),
                    ("op1", "Power Meter C",    14),
                    ("op2", "Valve 3",          20),
                    ("op2", "Moisture Sensor B",21),
                    ("op2", "Zone A Map",       27)
                };

                foreach (var (user, term, minutes) in seeded)
                    tracker.Record(new SearchEvent(user, term, t0.AddMinutes(minutes)));
            }
        }

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

        // ============================================================
        // Part 2 — Real-Time Command Stream & History
        // Every endpoint below reads/writes singleton in-memory data
        // structures, so state persists across requests for the life
        // of the process.
        // ============================================================
        var cmdStream = app.MapGroup("/api/command-stream");

        // -------- Aggregate statistics (drives the dashboard cards) --------
        cmdStream.MapGet("/stats", (TelemetryQueue tq, AlertPriorityQueue pq, CommandHistory ch,
                                    DeviceRegistry reg, TelemetryTimeline tl, ActiveAlertsSet set,
                                    SearchPatternTracker tracker) =>
            TypedResults.Ok(new
            {
                queueSize = tq.Count,
                priorityAlertCount = pq.Count,
                commandHistorySize = ch.Count,
                deviceCount = reg.Count,
                timelineReadingCount = tl.Count,
                activeAlertCount = set.Count,
                searchEvents = tracker.EventCount
            }));

        // -------- FIFO telemetry queue (ConcurrentQueue<T>) --------
        cmdStream.MapPost("/telemetry", (object packet, TelemetryQueue q, TelemetryTimeline tl) =>
        {
            q.Enqueue(packet);

            // Mirror the packet onto the sorted timeline so the chronological
            // log grows together with the queue during demos.
            if (packet is JsonElement el && el.ValueKind == JsonValueKind.Object)
            {
                string mac = el.TryGetProperty("deviceId", out var d) ? d.ToString() : "unknown";
                double value = el.TryGetProperty("value", out var v) && v.TryGetDouble(out var dv) ? dv : 0;
                string unit = el.TryGetProperty("unit", out var u) ? u.ToString() : "";
                tl.Add(new TimelineReading(mac, DateTime.UtcNow, value, unit));
            }

            return TypedResults.Ok(new { queued = true, size = q.Count });
        });

        cmdStream.MapGet("/telemetry", (TelemetryQueue q) => TypedResults.Ok(q.Snapshot()));

        cmdStream.MapGet("/telemetry/next", IResult (TelemetryQueue q) =>
            q.TryDequeue(out var p)
                ? TypedResults.Ok(new { packet = p, remaining = q.Count })
                : TypedResults.NotFound(new { message = "Queue empty." }));

        // -------- Priority alerts (critical bypasses the FIFO stream) --------
        cmdStream.MapPost("/alerts", (PriorityAlert alert, AlertPriorityQueue pq) =>
        {
            pq.Enqueue(alert);
            return TypedResults.Ok(new { queued = true, size = pq.Count });
        });

        cmdStream.MapGet("/alerts", (AlertPriorityQueue pq) => TypedResults.Ok(pq.Snapshot()));

        cmdStream.MapGet("/alerts/next", IResult (AlertPriorityQueue pq) =>
            pq.TryDequeue(out var a) && a is not null
                ? TypedResults.Ok(a)
                : TypedResults.NotFound(new { message = "No alerts queued." }));

        // -------- Duplicate prevention (HashSet<T>) --------
        cmdStream.MapPost("/alerts/raise", (string mac, string code, ActiveAlertsSet set) =>
        {
            var added = set.TryAdd(mac, code);
            return TypedResults.Ok(new
            {
                mac,
                code,
                wasDuplicate = !added,
                activeCount = set.Count
            });
        });

        cmdStream.MapGet("/alerts/active", (ActiveAlertsSet set) => TypedResults.Ok(set.All()));

        cmdStream.MapPost("/alerts/clear", (string mac, string code, ActiveAlertsSet set) =>
            TypedResults.Ok(new { cleared = set.Clear(mac, code), activeCount = set.Count }));

        // -------- Command history (Stack<T> + undo) --------
        cmdStream.MapPost("/commands", (ManualCommand cmd, CommandHistory h) =>
        {
            h.Push(cmd);
            return TypedResults.Ok(new { issued = true, historySize = h.Count });
        });

        cmdStream.MapGet("/commands", (CommandHistory h) => TypedResults.Ok(h.Snapshot()));

        cmdStream.MapGet("/commands/peek", IResult (CommandHistory h) =>
            h.Peek() is ManualCommand c
                ? TypedResults.Ok(c)
                : TypedResults.NotFound(new { message = "History empty." }));

        cmdStream.MapPost("/commands/undo", IResult (CommandHistory h) =>
        {
            var undone = h.Pop();
            return undone is null
                ? TypedResults.NotFound(new { message = "Nothing to undo." })
                : TypedResults.Ok(new { undone });
        });

        // -------- Device registry (Dictionary<string,SensorDevice>) --------
        cmdStream.MapGet("/devices", (DeviceRegistry reg) => TypedResults.Ok(reg.All()));

        cmdStream.MapPost("/devices", (SensorDevice device, DeviceRegistry reg) =>
        {
            reg.Register(device);
            return TypedResults.Ok(new { registered = true, count = reg.Count });
        });

        cmdStream.MapGet("/devices/{mac}", IResult (string mac, DeviceRegistry reg) =>
            reg.Find(mac) is SensorDevice d
                ? TypedResults.Ok(d)
                : TypedResults.NotFound(new { message = $"Unknown device '{mac}'." }));

        // -------- Sorted timeline (SortedDictionary<DateTime, ...>) --------
        cmdStream.MapPost("/timeline", (TimelineReading reading, TelemetryTimeline tl) =>
        {
            tl.Add(reading);
            return TypedResults.Ok(new { added = true, count = tl.Count });
        });

        cmdStream.MapGet("/timeline", IResult (DateTime? from, DateTime? to, TelemetryTimeline tl) =>
        {
            var f = from ?? DateTime.UtcNow.AddHours(-1);
            var t = to ?? DateTime.UtcNow;
            return TypedResults.Ok(tl.Range(f, t));
        });

        // -------- Predictive action & recommendation engine --------
        cmdStream.MapPost("/search", (SearchEvent ev, SearchPatternTracker tracker) =>
        {
            tracker.Record(ev);
            return TypedResults.Ok(new { recorded = true, total = tracker.EventCount });
        });

        cmdStream.MapGet("/suggestions", (string term, SearchPatternTracker tracker) =>
            TypedResults.Ok(new
            {
                term,
                suggestions = tracker.SuggestionsFor(term),
                analytics = tracker.Analytics()
            }));

        cmdStream.MapGet("/analytics", (SearchPatternTracker tracker) =>
            TypedResults.Ok(tracker.Analytics()));

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
