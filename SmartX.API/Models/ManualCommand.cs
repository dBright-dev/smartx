namespace SmartX.API.Models
{
    /// <summary>
    /// A manual override command issued by an operator against a device.
    /// Commands are pushed onto the LIFO <see cref="SmartX.API.DataStructures.CommandHistory"/>
    /// stack so the most recent action can be undone first.
    /// REFERENCE: Microsoft, 2026. Records (C# reference).
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records
    /// [Accessed 13 October 2026].
    /// </summary>
    public record ManualCommand(
        string MacAddress,
        string Command,
        DateTime IssuedAtUtc);
}
