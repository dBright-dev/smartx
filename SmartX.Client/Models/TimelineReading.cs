namespace SmartX.Client.Models
{
    /// <summary>Mirrors SmartX.API.DataStructures.TimelineReading.</summary>
    public record TimelineReading(
        string MacAddress,
        DateTime Timestamp,
        double Value,
        string Unit);

    /// <summary>One ranked recommendation from the predictive engine.</summary>
    public record TermSuggestion(
        string Term,
        int Occurrences,
        double Confidence,
        string Source);

    /// <summary>Engine analytics: how much history the frequency map has learned.</summary>
    public record SearchAnalytics(
        long RecordedEvents,
        int DistinctTerms,
        int DistinctTransitions,
        int TrackedUsers);

    /// <summary>Payload of GET /api/command-stream/suggestions.</summary>
    public record SuggestionResponse(
        string Term,
        List<TermSuggestion> Suggestions,
        SearchAnalytics Analytics);
}
