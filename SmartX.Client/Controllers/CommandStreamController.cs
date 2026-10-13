using Microsoft.AspNetCore.Mvc;
using SmartX.Client.Models;
using SmartX.Client.Services;

namespace SmartX.Client.Controllers
{
    /// <summary>
    /// Pillar 2 — Real-Time Command Stream &amp; History.
    /// All actions are async and delegate to <see cref="ICommandStreamClient"/>,
    /// which talks to the /api/command-stream endpoints on SmartX.API.
    /// </summary>
    public class CommandStreamController : Controller
    {
        private readonly ICommandStreamClient _api;
        private readonly ILogger<CommandStreamController> _logger;

        /// <summary>Demo operator identity for the predictive engine.</summary>
        private const string OperatorId = "operator";

        public CommandStreamController(ICommandStreamClient api,
                                       ILogger<CommandStreamController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // GET /CommandStream
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stats = await _api.GetStatsAsync();
            if (!stats.ApiReachable)
                TempData["Toast"] = "API unreachable \u2014 start SmartX.API (http://localhost:5042) and refresh.";

            var vm = new CommandStreamViewModel
            {
                Stats = stats,
                History = await _api.GetCommandHistoryAsync(),
                ActiveAlerts = await _api.GetActiveAlertsAsync(),
                Timeline = await _api.GetTimelineAsync(),
                Devices = await _api.GetDevicesAsync()
            };

            return View(vm);
        }

        // ---------- FIFO queue ----------
        [HttpPost]
        public async Task<IActionResult> EnqueueTelemetry(string deviceId, double value, string unit)
        {
            var ok = await _api.EnqueueTelemetryAsync(deviceId, value, unit ?? string.Empty);
            TempData["Toast"] = ok
                ? $"Telemetry packet from {deviceId} enqueued (FIFO queue grew)."
                : "Could not enqueue \u2014 is the API running?";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DequeueTelemetry()
        {
            var next = await _api.DequeueTelemetryAsync();
            TempData["Toast"] = next is null
                ? "Queue empty \u2014 nothing to drain."
                : $"Dequeued {next.Packet.GetRawText()} \u2014 {next.Remaining} packet(s) remaining.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Priority alerts ----------
        [HttpPost]
        public async Task<IActionResult> RaiseAlert(string mac, int severity, string message)
        {
            var ok = await _api.RaiseAlertAsync(mac, (AlertSeverity)severity, message);
            TempData["Toast"] = ok
                ? $"{(AlertSeverity)severity} alert queued for {mac}."
                : "Could not raise alert \u2014 is the API running?";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DequeueAlert()
        {
            var alert = await _api.DequeueAlertAsync();
            TempData["Toast"] = alert is null
                ? "No alerts queued."
                : $"Dequeued {alert.Severity} \u2014 {alert.Message} ({alert.MacAddress})";
            return RedirectToAction(nameof(Index));
        }

        // ---------- LIFO command history ----------
        [HttpPost]
        public async Task<IActionResult> IssueCommand(string mac, string command)
        {
            var ok = await _api.IssueCommandAsync(mac, command);
            TempData["Toast"] = ok
                ? $"Command '{command}' issued to {mac}."
                : "Could not issue command \u2014 is the API running?";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Undo()
        {
            var undone = await _api.UndoCommandAsync();
            TempData["Toast"] = undone is not null
                ? $"Undid '{undone.Command}' on {undone.MacAddress} (LIFO \u2014 last action reverted first)."
                : "Nothing to undo.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- HashSet duplicate prevention ----------
        [HttpPost]
        public async Task<IActionResult> RaiseActiveAlert(string mac, string code)
        {
            var result = await _api.RaiseActiveAlertAsync(mac, code);
            TempData["Toast"] = result is null
                ? "Could not raise active alert \u2014 is the API running?"
                : result.WasDuplicate
                    ? $"DUPLICATE suppressed: {result.Code} on {result.Mac} already active (HashSet count still {result.ActiveCount})."
                    : $"Active alert added: {result.Code} on {result.Mac} (HashSet count {result.ActiveCount}).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Predictive engine ----------
        /// <summary>
        /// Records the search in the frequency map and immediately returns ranked
        /// follow-up suggestions. Called by the dashboard via fetch().
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new SuggestionResponse(string.Empty, new List<TermSuggestion>(),
                    new SearchAnalytics(0, 0, 0, 0)));

            await _api.RecordSearchAsync(OperatorId, term);
            var response = await _api.GetSuggestionsAsync(term);
            return Json(response ?? new SuggestionResponse(term, new List<TermSuggestion>(),
                new SearchAnalytics(0, 0, 0, 0)));
        }
    }
}
