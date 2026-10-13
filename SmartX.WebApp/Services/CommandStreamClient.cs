using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartX.WebApp.Models;

namespace SmartX.WebApp.Services
{
    /// <summary>
    /// Concrete <see cref="ICommandStreamClient"/> for the MAUI app — the twin of
    /// SmartX.Client.Services.CommandStreamClient. Base address comes from
    /// <see cref="ApiConstants.BaseUrl"/> via the typed HttpClient registration.
    /// </summary>
    public class CommandStreamClient : ICommandStreamClient
    {
        private const string Base = "/api/command-stream";
        private readonly HttpClient _http;
        private readonly ILogger<CommandStreamClient> _logger;

        public CommandStreamClient(HttpClient http, ILogger<CommandStreamClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ---------- Stats ----------
        public async Task<CommandStreamStats> GetStatsAsync(CancellationToken ct = default)
        {
            try
            {
                var s = await _http.GetFromJsonAsync<CommandStreamStats>($"{Base}/stats", ct);
                return s ?? new CommandStreamStats { ApiReachable = false };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("Command-stream API unreachable: {Message}", ex.Message);
                return new CommandStreamStats { ApiReachable = false };
            }
        }

        // ---------- FIFO telemetry queue ----------
        public async Task<bool> EnqueueTelemetryAsync(string deviceId, double value, string unit,
                                                      CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{Base}/telemetry", new
                {
                    deviceId,
                    value,
                    unit,
                    timestamp = DateTime.UtcNow
                }, ct);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("Enqueue failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<TelemetryDequeueResult?> DequeueTelemetryAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await _http.GetAsync($"{Base}/telemetry/next", ct);
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                return await res.Content.ReadFromJsonAsync<TelemetryDequeueResult>(
                    cancellationToken: ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("Dequeue failed: {Message}", ex.Message);
                return null;
            }
        }

        // ---------- Priority alerts ----------
        public async Task<bool> RaiseAlertAsync(string macAddress, AlertSeverity severity,
                                                string message, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{Base}/alerts", new
                {
                    macAddress,
                    severity,
                    message,
                    raisedAtUtc = DateTime.UtcNow
                }, ct);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("RaiseAlert failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<PriorityAlert?> DequeueAlertAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await _http.GetAsync($"{Base}/alerts/next", ct);
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                return await res.Content.ReadFromJsonAsync<PriorityAlert>(
                    cancellationToken: ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("DequeueAlert failed: {Message}", ex.Message);
                return null;
            }
        }

        // ---------- LIFO command history ----------
        public async Task<List<ManualCommand>> GetCommandHistoryAsync(CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ManualCommand>>($"{Base}/commands", ct)
                       ?? new List<ManualCommand>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("GetCommandHistory failed: {Message}", ex.Message);
                return new List<ManualCommand>();
            }
        }

        public async Task<bool> IssueCommandAsync(string macAddress, string command,
                                                  CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{Base}/commands", new
                {
                    macAddress,
                    command,
                    issuedAtUtc = DateTime.UtcNow
                }, ct);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("IssueCommand failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<ManualCommand?> UndoCommandAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsync($"{Base}/commands/undo", null, ct);
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                var wrapper = await res.Content.ReadFromJsonAsync<UndoResult>(
                    cancellationToken: ct);
                return wrapper?.Undone;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("Undo failed: {Message}", ex.Message);
                return null;
            }
        }

        // ---------- HashSet duplicate prevention ----------
        public async Task<DuplicateAlertResult?> RaiseActiveAlertAsync(string macAddress, string code,
                                                                      CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsync(
                    $"{Base}/alerts/raise?mac={Uri.EscapeDataString(macAddress)}&code={Uri.EscapeDataString(code)}",
                    null, ct);
                if (!res.IsSuccessStatusCode) return null;
                return await res.Content.ReadFromJsonAsync<DuplicateAlertResult>(
                    cancellationToken: ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("RaiseActiveAlert failed: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<List<string>> GetActiveAlertsAsync(CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<string>>($"{Base}/alerts/active", ct)
                       ?? new List<string>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                return new List<string>();
            }
        }

        // ---------- Dictionary & sorted timeline ----------
        public async Task<List<SensorDevice>> GetDevicesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<SensorDevice>>($"{Base}/devices", ct)
                       ?? new List<SensorDevice>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("GetDevices failed: {Message}", ex.Message);
                return new List<SensorDevice>();
            }
        }

        public async Task<List<TimelineReading>> GetTimelineAsync(CancellationToken ct = default)
        {
            try
            {
                var readings = await _http.GetFromJsonAsync<List<TimelineReading>>(
                    $"{Base}/timeline?from={DateTime.UtcNow.AddHours(-6):o}&to={DateTime.UtcNow:o}", ct);
                return readings ?? new List<TimelineReading>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("GetTimeline failed: {Message}", ex.Message);
                return new List<TimelineReading>();
            }
        }

        // ---------- Predictive engine ----------
        public async Task<bool> RecordSearchAsync(string userId, string term,
                                                  CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{Base}/search", new
                {
                    userId,
                    term,
                    whenUtc = DateTime.UtcNow
                }, ct);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("RecordSearch failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<SuggestionResponse?> GetSuggestionsAsync(string term,
                                                                  CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<SuggestionResponse>(
                    $"{Base}/suggestions?term={Uri.EscapeDataString(term)}", ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning("GetSuggestions failed: {Message}", ex.Message);
                return null;
            }
        }
    }
}
