using SmartX.WebApp.Models;

namespace SmartX.WebApp.Services
{
    /// <summary>
    /// Typed client for the Part 2 /api/command-stream endpoints — the MAUI twin
    /// of SmartX.Client.Services.ICommandStreamClient. Every method degrades
    /// gracefully (returns null/empty) when the API is offline.
    /// REFERENCE: Microsoft, 2026. Make HTTP requests using IHttpClientFactory.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
    /// [Accessed 13 October 2026].
    /// </summary>
    public interface ICommandStreamClient
    {
        Task<CommandStreamStats> GetStatsAsync(CancellationToken ct = default);

        Task<bool> EnqueueTelemetryAsync(string deviceId, double value, string unit,
                                          CancellationToken ct = default);
        Task<TelemetryDequeueResult?> DequeueTelemetryAsync(CancellationToken ct = default);

        Task<bool> RaiseAlertAsync(string macAddress, AlertSeverity severity, string message,
                                   CancellationToken ct = default);
        Task<PriorityAlert?> DequeueAlertAsync(CancellationToken ct = default);

        Task<List<ManualCommand>> GetCommandHistoryAsync(CancellationToken ct = default);
        Task<bool> IssueCommandAsync(string macAddress, string command,
                                     CancellationToken ct = default);
        Task<ManualCommand?> UndoCommandAsync(CancellationToken ct = default);

        Task<DuplicateAlertResult?> RaiseActiveAlertAsync(string macAddress, string code,
                                                          CancellationToken ct = default);
        Task<List<string>> GetActiveAlertsAsync(CancellationToken ct = default);

        Task<List<SensorDevice>> GetDevicesAsync(CancellationToken ct = default);
        Task<List<TimelineReading>> GetTimelineAsync(CancellationToken ct = default);

        Task<bool> RecordSearchAsync(string userId, string term,
                                     CancellationToken ct = default);
        Task<SuggestionResponse?> GetSuggestionsAsync(string term,
                                                      CancellationToken ct = default);
    }
}
