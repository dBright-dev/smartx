using SmartX.Client.Models;

namespace SmartX.Client.Services
{
    /// <summary>
    /// Typed client for the Part 2 /api/command-stream endpoints.
    /// Every method degrades gracefully (returns null/empty) when the API is
    /// offline so the MVC dashboard still renders instead of throwing.
    /// REFERENCE: Microsoft, 2026. Typed HTTP clients in ASP.NET Core.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
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
