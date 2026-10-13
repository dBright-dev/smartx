using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartX.WebApp.Models;
using SmartX.WebApp.Services;

namespace SmartX.WebApp.ViewModels
{
    /// <summary>
    /// Part 2 — Real-Time Command Stream &amp; History for the MAUI app.
    /// Mirrors SmartX.Client's CommandStreamController/Index view-model state:
    /// queue stats, undo stack, active-alert set, timeline and suggestions.
    /// REFERENCE: Microsoft, 2026. CommunityToolkit.Mvvm ObservableObject.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableobject
    /// [Accessed 13 October 2026].
    /// </summary>
    public partial class CommandStreamViewModel : ObservableObject
    {
        private readonly ICommandStreamClient _api;
        private readonly ILogger<CommandStreamViewModel> _logger;

        /// <summary>Demo operator identity for the predictive engine.</summary>
        private const string OperatorId = "operator";

        public CommandStreamViewModel(ICommandStreamClient api, ILogger<CommandStreamViewModel> logger)
        {
            _api = api;
            _logger = logger;

            // Load dashboard state at start-up (same pattern as IngestionViewModel).
            _ = RefreshAsync();
        }

        // ------- Aggregate counters (GET /stats) -------

        [ObservableProperty] private int _queueSize;
        [ObservableProperty] private int _priorityAlertCount;
        [ObservableProperty] private int _commandHistorySize;
        [ObservableProperty] private int _activeAlertCount;
        [ObservableProperty] private int _deviceCount;
        [ObservableProperty] private long _searchEvents;
        [ObservableProperty] private bool _apiReachable = true;

        // ------- Collections -------

        [ObservableProperty] private List<ManualCommand> _commandHistory = new();
        [ObservableProperty] private List<string> _activeAlerts = new();
        [ObservableProperty] private List<TimelineReading> _timeline = new();
        [ObservableProperty] private List<SensorDevice> _devices = new();
        [ObservableProperty] private List<TermSuggestion> _suggestions = new();

        // ------- Form state -------

        [ObservableProperty] private string _enqueueDeviceId = "AA:BB:CC:00:00:01";
        [ObservableProperty] private string _enqueueValue = "23.5";
        [ObservableProperty] private string _enqueueUnit = "%";

        [ObservableProperty] private string _alertMac = "AA:BB:CC:00:00:01";
        [ObservableProperty] private int _selectedSeverityIndex = 1; // Warning (matches the web form default)
        [ObservableProperty] private string _alertMessage = "Filter pressure high";

        [ObservableProperty] private string _commandMac = "AA:BB:CC:00:00:02";
        [ObservableProperty] private string _commandText = "Open valve";

        [ObservableProperty] private string _activeAlertMac = "AA:BB:CC:00:00:01";
        [ObservableProperty] private string _activeAlertCode = "DISCONNECT";

        [ObservableProperty] private string _searchTerm = string.Empty;
        [ObservableProperty] private string _suggestionSummary = string.Empty;

        // ------- Status bar -------

        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private Color _statusColor = Colors.Gray;
        [ObservableProperty] private bool _isBusy;

        /// <summary>Picker items for severity — index drives <see cref="SeverityFromIndex"/>.</summary>
        public List<string> Severities { get; } = new() { "Critical", "Warning", "Info" };

        private static AlertSeverity SeverityFromIndex(int index) => index switch
        {
            0 => AlertSeverity.Critical,
            1 => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };

        // ------- Commands -------

        /// <summary>Reloads every panel: stats, queue, history, set, timeline, registry.</summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                var stats = await _api.GetStatsAsync();
                ApiReachable = stats.ApiReachable;
                QueueSize = stats.QueueSize;
                PriorityAlertCount = stats.PriorityAlertCount;
                CommandHistorySize = stats.CommandHistorySize;
                ActiveAlertCount = stats.ActiveAlertCount;
                DeviceCount = stats.DeviceCount;
                SearchEvents = stats.SearchEvents;

                CommandHistory = await _api.GetCommandHistoryAsync();
                ActiveAlerts = await _api.GetActiveAlertsAsync();
                Timeline = await _api.GetTimelineAsync();
                Devices = await _api.GetDevicesAsync();

                StatusMessage = ApiReachable
                    ? $"Refreshed — queue {QueueSize}, alerts {PriorityAlertCount}, history {CommandHistorySize}."
                    : "API unreachable — start SmartX.API on port 5042.";
                StatusColor = ApiReachable ? Colors.Green : Colors.Red;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh failed");
                StatusMessage = "Refresh failed: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task EnqueueTelemetryAsync()
        {
            if (!double.TryParse(EnqueueValue, out var value))
            {
                StatusMessage = "Value must be a number.";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                var ok = await _api.EnqueueTelemetryAsync(EnqueueDeviceId, value, EnqueueUnit);
                StatusMessage = ok
                    ? $"Packet from {EnqueueDeviceId} enqueued (FIFO)."
                    : "Enqueue failed — is the API running?";
                StatusColor = ok ? Colors.Green : Colors.Red;
                if (ok) await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enqueue failed");
                StatusMessage = "Enqueue error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task DequeueTelemetryAsync()
        {
            IsBusy = true;
            try
            {
                var next = await _api.DequeueTelemetryAsync();
                StatusMessage = next is null
                    ? "Queue empty — nothing to drain."
                    : $"Dequeued {next.Packet.GetRawText()} — {next.Remaining} left.";
                StatusColor = next is null ? Colors.Gray : Colors.Green;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dequeue failed");
                StatusMessage = "Dequeue error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task RaiseAlertAsync()
        {
            IsBusy = true;
            try
            {
                var severity = SeverityFromIndex(SelectedSeverityIndex);
                var ok = await _api.RaiseAlertAsync(AlertMac, severity, AlertMessage);
                StatusMessage = ok
                    ? $"{severity} alert queued for {AlertMac}."
                    : "Raise failed — is the API running?";
                StatusColor = ok ? Colors.Green : Colors.Red;
                if (ok) await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RaiseAlert failed");
                StatusMessage = "Raise error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task DequeueAlertAsync()
        {
            IsBusy = true;
            try
            {
                var alert = await _api.DequeueAlertAsync();
                StatusMessage = alert is null
                    ? "No alerts queued."
                    : $"Dequeued {alert.Severity} — {alert.Message} ({alert.MacAddress})";
                StatusColor = alert is null ? Colors.Gray : Colors.Orange;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DequeueAlert failed");
                StatusMessage = "Dequeue alert error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task IssueAsync()
        {
            if (string.IsNullOrWhiteSpace(CommandText))
            {
                StatusMessage = "Enter a command first.";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                var ok = await _api.IssueCommandAsync(CommandMac, CommandText);
                StatusMessage = ok
                    ? $"Command '{CommandText}' issued to {CommandMac}."
                    : "Issue failed — is the API running?";
                StatusColor = ok ? Colors.Green : Colors.Red;
                if (ok) await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IssueCommand failed");
                StatusMessage = "Issue error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task UndoAsync()
        {
            IsBusy = true;
            try
            {
                var undone = await _api.UndoCommandAsync();
                StatusMessage = undone is not null
                    ? $"Undid '{undone.Command}' on {undone.MacAddress} (LIFO)."
                    : "Nothing to undo.";
                StatusColor = undone is not null ? Colors.Green : Colors.Gray;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo failed");
                StatusMessage = "Undo error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task RaiseActiveAlertAsync()
        {
            IsBusy = true;
            try
            {
                var result = await _api.RaiseActiveAlertAsync(ActiveAlertMac, ActiveAlertCode);
                if (result is null)
                {
                    StatusMessage = "Raise failed — is the API running?";
                    StatusColor = Colors.Red;
                }
                else if (result.WasDuplicate)
                {
                    // HashSet suppressed the duplicate — the whole point of the demo.
                    StatusMessage = $"DUPLICATE suppressed: {result.Code} on {result.Mac} (count still {result.ActiveCount}).";
                    StatusColor = Colors.Orange;
                }
                else
                {
                    StatusMessage = $"Active alert added: {result.Code} on {result.Mac} (count {result.ActiveCount}).";
                    StatusColor = Colors.Green;
                }
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RaiseActiveAlert failed");
                StatusMessage = "Raise active alert error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Records the search in the frequency map, then loads ranked follow-ups
        /// from the predictive engine (same flow as the MVC dashboard).
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                StatusMessage = "Enter a search term first.";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                await _api.RecordSearchAsync(OperatorId, SearchTerm);
                var response = await _api.GetSuggestionsAsync(SearchTerm);

                Suggestions = response?.Suggestions ?? new List<TermSuggestion>();
                SuggestionSummary = response is null
                    ? "Suggestions unavailable."
                    : $"Learned {response.Analytics.RecordedEvents} events across " +
                      $"{response.Analytics.DistinctTerms} terms " +
                      $"({response.Analytics.DistinctTransitions} transitions).";

                StatusMessage = Suggestions.Count > 0
                    ? $"Top follow-up for '{SearchTerm}': {Suggestions[0].Term} ({Suggestions[0].Confidence}%)."
                    : $"Recorded '{SearchTerm}' — no follow-ups known yet.";
                StatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed");
                StatusMessage = "Search error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }
    }
}
