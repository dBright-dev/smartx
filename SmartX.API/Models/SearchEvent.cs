namespace SmartX.API.Models
{
    /// <summary>
    /// A single search performed by an operator. Recorded by the predictive
    /// action engine (<see cref="SmartX.API.DataStructures.SearchPatternTracker"/>)
    /// to learn which terms tend to be searched in sequence.
    /// REFERENCE: Microsoft, 2026. Records (C# reference).
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records
    /// [Accessed 13 October 2026].
    /// </summary>
    public record SearchEvent(
        string UserId,
        string Term,
        DateTime WhenUtc);
}
