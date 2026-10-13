namespace SmartX.API.Models
{
    /// <summary>
    /// Severity of an alert. The numeric values double as the priority passed to
    /// <see cref="System.Collections.Generic.PriorityQueue{TElement,TPriority}"/>:
    /// a LOWER number is dequeued first, so Critical (1) always beats Warning (2)
    /// and Info (3).
    /// </summary>
    public enum AlertSeverity
    {
        Info = 3,
        Warning = 2,
        Critical = 1
    }

    /// <summary>
    /// An alert raised by a device. Stored on the
    /// <see cref="SmartX.API.DataStructures.AlertPriorityQueue"/> so critical
    /// alerts bypass the standard FIFO telemetry stream.
    /// REFERENCE: Microsoft, 2026. Records (C# reference).
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records
    /// [Accessed 13 October 2026].
    /// </summary>
    public record PriorityAlert(
        string MacAddress,
        AlertSeverity Severity,
        string Message,
        DateTime RaisedAtUtc);
}
