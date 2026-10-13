# Smart-X Gateway

An IoT gateway solution built with **.NET 10 / ASP.NET Core**, demonstrating
real-time telemetry ingestion and a live command-stream processing engine built
on classic data structures.

| Pillar | Module | Status |
|---|---|---|
| 1 | Sensor Data Ingestion & Telemetry | ✅ Active |
| 2 | Real-Time Command Stream & History | ✅ **Active (Part 2)** |
| 3 | Network Topology & Mesh Routing | ⏳ Disabled (Final PoE) |

## Solution layout

```
smartx/
├── SmartX.API/               # Minimal API — ingestion + command stream
│   ├── DataStructures/       # (Part 2) the 7 required data structures
│   ├── Models/               # SensorDevice, TelemetryPacket, ManualCommand, ...
│   ├── Services/             # TelemetryBatchProcessor, DeploymentTreeValidator
│   └── Program.cs            # DI registrations + /api/command-stream endpoints
├── SmartX.Client/            # MVC dashboard (the web UI you demo)
│   ├── Controllers/          # Home, Sensors, CommandStream
│   ├── Services/             # ICommandStreamClient (typed HttpClient)
│   └── Views/                # Landing page + Command Stream dashboard
├── SmartX.WebApp/            # .NET MAUI companion app
├── docker-compose.yml
└── README.md
```

## Prerequisites

- .NET SDK **10.0** (`dotnet --version`)
- (Optional) Docker + Docker Compose for containerised runs
- curl **or** PowerShell for the API tests below

## Running locally

Open two terminals from the repository root:

```powershell
# Terminal 1 — API (http://localhost:5042)
dotnet run --project .\SmartX.API --no-launch-profile --urls http://localhost:5042

# Terminal 2 — MVC client (http://localhost:5149)
dotnet run --project .\SmartX.Client --no-launch-profile --urls http://localhost:5149
```

Then browse to **http://localhost:5149** — the landing page shows
**Pillar 2 → “Open Command Stream”** as *Active*, Pillar 3 stays disabled.

> **Testing tip (PowerShell):** `curl.exe -d '{...}'` mangles the double quotes
> inside JSON when PowerShell forwards the arguments, which produces
> `400 Bad Request` with an empty body. Use the `Invoke-RestMethod` snippets
> shown below (or write the body to a file and pass `-d @body.json`). In
> *Git Bash* the plain `curl` commands work exactly as written.

---

## Part 1 — quick ingestion check

```powershell
# Health
Invoke-RestMethod http://localhost:5042/health

# Register a device
Invoke-RestMethod -Method Post http://localhost:5042/api/devices/register `
  -ContentType 'application/json' `
  -Body '{"macAddress":"AA:BB:CC:00:00:99","deploymentLocation":"Test Bench","zone":"Z9","nodeId":"NODE-99","category":0}'

# Send typed telemetry
Invoke-RestMethod -Method Post http://localhost:5042/api/telemetry/float `
  -ContentType 'application/json' `
  -Body '{"deviceId":"AA:BB:CC:00:00:99","value":23.5,"unit":"C"}'
```

---

## Part 2 — Testing the data structures

### Data-structure map (rubric)

| Required structure | Class | Endpoint(s) |
|---|---|---|
| `Queue<T>` / `ConcurrentQueue<T>` (FIFO) | `DataStructures/TelemetryQueue.cs` | `POST /api/command-stream/telemetry`, `GET .../telemetry/next` |
| `PriorityQueue<TElement,TPriority>` | `DataStructures/AlertPriorityQueue.cs` | `POST .../alerts`, `GET .../alerts/next` |
| `Stack<T>` (undo, LIFO) | `DataStructures/CommandHistory.cs` | `POST .../commands`, `POST .../commands/undo` |
| `Dictionary<TKey,TValue>` (O(1)) | `DataStructures/DeviceRegistry.cs` | `GET .../devices/{mac}` |
| `SortedDictionary<TKey,TValue>` | `DataStructures/TelemetryTimeline.cs` | `GET .../timeline?from=&to=` |
| `HashSet<T>` (dedup) | `DataStructures/ActiveAlertsSet.cs` | `POST .../alerts/raise` |
| Frequency map + sorting (engine) | `DataStructures/SearchPatternTracker.cs` | `POST .../search`, `GET .../suggestions` |

Base URL for every example: `http://localhost:5042`

### 1. Queue (FIFO)

```bash
# Enqueue three telemetry packets
curl -X POST http://localhost:5042/api/command-stream/telemetry \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"AA:BB:CC:00:00:01","value":23.5,"unit":"%"}'
curl -X POST http://localhost:5042/api/command-stream/telemetry \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"AA:BB:CC:00:00:02","value":4.1,"unit":"bar"}'

# Drain them — the FIRST packet in comes out first
curl http://localhost:5042/api/command-stream/telemetry/next
# → {"packet":{"deviceId":"AA:BB:CC:00:00:01",...},"remaining":1}
```

```powershell
$body = '{"deviceId":"AA:BB:CC:00:00:01","value":23.5,"unit":"%"}'
Invoke-RestMethod -Method Post http://localhost:5042/api/command-stream/telemetry `
  -ContentType 'application/json' -Body $body
Invoke-RestMethod http://localhost:5042/api/command-stream/telemetry/next
```

### 2. Priority Queue — how to simulate a high-priority alert

Raise a **Warning** first, then a **Critical**. Dequeueing returns the
**Critical** alert even though it was enqueued last:

```bash
curl -X POST http://localhost:5042/api/command-stream/alerts \
  -H "Content-Type: application/json" \
  -d '{"macAddress":"AA:BB:CC:00:00:01","severity":2,"message":"Filter pressure high","raisedAtUtc":"2026-10-13T10:00:00Z"}'

curl -X POST http://localhost:5042/api/command-stream/alerts \
  -H "Content-Type: application/json" \
  -d '{"macAddress":"AA:BB:CC:00:00:03","severity":1,"message":"Power spike","raisedAtUtc":"2026-10-13T10:00:05Z"}'

# Dequeue — returns the Critical alert first (priority 1 < 2)
curl http://localhost:5042/api/command-stream/alerts/next
# → {"macAddress":"AA:BB:CC:00:00:03","severity":1,"message":"Power spike",...}
```

```powershell
Invoke-RestMethod -Method Post http://localhost:5042/api/command-stream/alerts `
  -ContentType 'application/json' `
  -Body '{"macAddress":"AA:BB:CC:00:00:01","severity":2,"message":"Filter pressure high","raisedAtUtc":"2026-10-13T10:00:00Z"}'
Invoke-RestMethod -Method Post http://localhost:5042/api/command-stream/alerts `
  -ContentType 'application/json' `
  -Body '{"macAddress":"AA:BB:CC:00:00:03","severity":1,"message":"Power spike","raisedAtUtc":"2026-10-13T10:00:05Z"}'
Invoke-RestMethod http://localhost:5042/api/command-stream/alerts/next   # → Critical
```

**Severity values:** `Critical = 1`, `Warning = 2`, `Info = 3` (lower dequeues first).

### 3. Stack (undo / LIFO)

```bash
# Push two commands
curl -X POST http://localhost:5042/api/command-stream/commands \
  -H "Content-Type: application/json" \
  -d '{"macAddress":"AA:BB:CC:00:00:02","command":"Open valve","issuedAtUtc":"2026-10-13T10:00:00Z"}'
curl -X POST http://localhost:5042/api/command-stream/commands \
  -H "Content-Type: application/json" \
  -d '{"macAddress":"AA:BB:CC:00:00:02","command":"Shut down all valves","issuedAtUtc":"2026-10-13T10:01:00Z"}'

# Undo — returns the LAST command issued (LIFO)
curl -X POST http://localhost:5042/api/command-stream/commands/undo
# → {"undone":{"command":"Shut down all valves",...}}

# Remaining history (newest first) and the next undo target
curl http://localhost:5042/api/command-stream/commands
curl http://localhost:5042/api/command-stream/commands/peek
```

### 4. Dictionary — O(1) device lookup

```bash
# Known MAC → 200 with the device (instant hash lookup)
curl http://localhost:5042/api/command-stream/devices/AA:BB:CC:00:00:01

# Unknown MAC → 404
curl -i http://localhost:5042/api/command-stream/devices/FF:FF:FF:FF:FF:FF

# Register another device into the dictionary
curl -X POST http://localhost:5042/api/command-stream/devices \
  -H "Content-Type: application/json" \
  -d '{"macAddress":"AA:BB:CC:00:00:05","deploymentLocation":"West Field","zone":"Zone D","nodeId":"NODE-05","category":0}'
```

### 5. SortedDictionary — chronological timeline

```bash
curl "http://localhost:5042/api/command-stream/timeline?from=2026-10-13T00:00:00Z&to=2026-10-13T23:59:59Z"
```

```powershell
$from = (Get-Date).ToUniversalTime().AddHours(-6).ToString('yyyy-MM-ddTHH:mm:ssZ')
$to   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
Invoke-RestMethod "http://localhost:5042/api/command-stream/timeline?from=$from&to=$to"
```

Readings are returned **sorted by timestamp**; enqueueing a telemetry packet
(§1) also appends it to the timeline automatically.

### 6. Set — duplicate prevention

```bash
# First call: wasDuplicate = false
curl -X POST "http://localhost:5042/api/command-stream/alerts/raise?mac=AA:BB:CC:00:00:01&code=DISCONNECT"
# → {"mac":"...","code":"DISCONNECT","wasDuplicate":false,"activeCount":1}

# Second call: wasDuplicate = true — same alert is NOT re-processed
curl -X POST "http://localhost:5042/api/command-stream/alerts/raise?mac=AA:BB:CC:00:00:01&code=DISCONNECT"
# → {"mac":"...","code":"DISCONNECT","wasDuplicate":true,"activeCount":1}

# List the unique set / clear one entry
curl http://localhost:5042/api/command-stream/alerts/active
curl -X POST "http://localhost:5042/api/command-stream/alerts/clear?mac=AA:BB:CC:00:00:01&code=DISCONNECT"
```

### 7. Predictive Action & Recommendation Engine

The engine keeps a **frequency map of term transitions** (bigrams) plus a
global popularity map, and ranks follow-ups by occurrence count with a
confidence percentage. A short demo history is seeded at startup, so the very
first query already returns results.

```bash
# Record searches
curl -X POST http://localhost:5042/api/command-stream/search \
  -H "Content-Type: application/json" \
  -d '{"userId":"op1","term":"Moisture Sensor B","whenUtc":"2026-10-13T10:00:00Z"}'
curl -X POST http://localhost:5042/api/command-stream/search \
  -H "Content-Type: application/json" \
  -d '{"userId":"op1","term":"Pump A","whenUtc":"2026-10-13T10:00:05Z"}'

# Ask what usually follows the first term
curl "http://localhost:5042/api/command-stream/suggestions?term=Moisture%20Sensor%20B"
# → {"term":"Moisture Sensor B",
#    "suggestions":[
#      {"term":"Pump A","occurrences":2,"confidence":50,"source":"co-occurrence"}, ...],
#    "analytics":{"recordedEvents":12,"distinctTerms":5,"distinctTransitions":4,"trackedUsers":2}}
```

```powershell
Invoke-RestMethod -Method Post http://localhost:5042/api/command-stream/search `
  -ContentType 'application/json' `
  -Body '{"userId":"op1","term":"Moisture Sensor B","whenUtc":"2026-10-13T10:00:00Z"}'
Invoke-RestMethod -Method Post http://localhost:5042/api/command-stream/search `
  -ContentType 'application/json' `
  -Body '{"userId":"op1","term":"Pump A","whenUtc":"2026-10-13T10:00:05Z"}'
Invoke-RestMethod "http://localhost:5042/api/command-stream/suggestions?term=Moisture%20Sensor%20B"
```

If a term has no transition history yet, the engine falls back to the
**popularity** frequency map so the operator always sees ranked suggestions.

---

## Testing Part 2 through the UI (demo script)

1. **Landing page** — Pillar 2 shows *Active*; click **Open Command Stream**.
2. Watch the six counter cards (queue size, alerts, history depth, …).
3. **Queue:** enqueue 3 packets, then *Drain next packet* — each toast shows the
   packet that has been waiting longest (FIFO).
4. **High-priority alert:** raise a *Warning*, then a *Critical*, then click
   *Dequeue highest-priority alert* — the **Critical** alert comes out first.
5. **Undo:** issue two commands, watch the history table (newest on top), click
   *Undo last command* — only the newest command is reverted (LIFO).
6. **Duplicates:** raise `AA:BB:CC:00:00:01 / DISCONNECT` twice — the second
   toast reads `DUPLICATE suppressed … (HashSet count 1)`.
7. **Suggestions:** search `Moisture Sensor B`, then `Pump A`, then search
   `Moisture Sensor B` again — the badges now show `Pump A` ranked first with
   its confidence %. The analytics line shows how many events the engine has
   learned.
8. **Timeline & registry:** the bottom panels show chronologically sorted
   readings (`SortedDictionary`) and the O(1) device dictionary.

## API reference (Part 2)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/command-stream/stats` | All dashboard counters in one call |
| POST | `/api/command-stream/telemetry` | Enqueue packet (FIFO) |
| GET | `/api/command-stream/telemetry` | Queue snapshot |
| GET | `/api/command-stream/telemetry/next` | Dequeue oldest packet |
| POST | `/api/command-stream/alerts` | Raise priority alert |
| GET | `/api/command-stream/alerts` | Alerts ordered by priority |
| GET | `/api/command-stream/alerts/next` | Dequeue highest-priority alert |
| POST | `/api/command-stream/alerts/raise` | Add to active-alert **set** (dedup) |
| GET | `/api/command-stream/alerts/active` | List unique active alerts |
| POST | `/api/command-stream/alerts/clear` | Remove an active alert |
| POST | `/api/command-stream/commands` | Push manual command (stack) |
| GET | `/api/command-stream/commands` | History snapshot (newest first) |
| GET | `/api/command-stream/commands/peek` | Top of the stack |
| POST | `/api/command-stream/commands/undo` | Pop the stack (undo) |
| GET | `/api/command-stream/devices` | All registered devices |
| POST | `/api/command-stream/devices` | Register device (dictionary) |
| GET | `/api/command-stream/devices/{mac}` | O(1) lookup |
| POST | `/api/command-stream/timeline` | Add sorted reading |
| GET | `/api/command-stream/timeline?from&to` | Chronological range query |
| POST | `/api/command-stream/search` | Record search event |
| GET | `/api/command-stream/suggestions?term=` | Ranked follow-up terms |
| GET | `/api/command-stream/analytics` | Engine frequency-map analytics |

## Running with Docker

```bash
docker compose up --build
# API    → http://localhost:5042
# Client → http://localhost:5149
```

`docker-compose.yml` wires the client to the API with the
`ApiSettings__BaseUrl=http://api:8080` environment variable, so the same image
also works outside compose.

## References

- Microsoft, 2026a. *ConcurrentQueue\<T\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1 [Accessed 13 October 2026].
- Microsoft, 2026b. *PriorityQueue\<TElement,TPriority\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2 [Accessed 13 October 2026].
- Microsoft, 2026c. *Stack\<T\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1 [Accessed 13 October 2026].
- Microsoft, 2026d. *Dictionary\<TKey,TValue\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2 [Accessed 13 October 2026].
- Microsoft, 2026e. *SortedDictionary\<TKey,TValue\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2 [Accessed 13 October 2026].
- Microsoft, 2026f. *HashSet\<T\> Class*. [online] Available at: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1 [Accessed 13 October 2026].
- Microsoft, 2026g. *Typed HTTP clients in ASP.NET Core*. [online] Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests [Accessed 13 October 2026].
- Microsoft, 2026h. *Dependency injection in ASP.NET Core*. [online] Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection [Accessed 13 October 2026].

