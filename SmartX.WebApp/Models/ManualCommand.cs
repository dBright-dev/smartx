namespace SmartX.WebApp.Models
{
    /// <summary>Mirrors SmartX.API.Models.ManualCommand (LIFO undo stack).</summary>
    public record ManualCommand(
        string MacAddress,
        string Command,
        DateTime IssuedAtUtc);

    /// <summary>Severity drives priority — lower number dequeues first.</summary>
    public enum AlertSeverity
    {
        Info = 3,
        Warning = 2,
        Critical = 1
    }

    /// <summary>Mirrors SmartX.API.Models.PriorityAlert.</summary>
    public record PriorityAlert(
        string MacAddress,
        AlertSeverity Severity,
        string Message,
        DateTime RaisedAtUtc);

    /// <summary>Response of POST /alerts/raise — proves HashSet duplicate suppression.</summary>
    public record DuplicateAlertResult(
        string Mac,
        string Code,
        bool WasDuplicate,
        int ActiveCount);

    /// <summary>Response wrapper of POST /commands/undo.</summary>
    public record UndoResult(ManualCommand Undone);

    /// <summary>Response of GET /telemetry/next.</summary>
    public record TelemetryDequeueResult(
        System.Text.Json.JsonElement Packet,
        int Remaining);
}
