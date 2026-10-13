using System.Collections.Concurrent;
using SmartX.API.Models;

namespace SmartX.API.DataStructures
{
    /// <summary>
    /// A single ranked recommendation produced by the predictive engine.
    /// <c>Confidence</c> is a 0–100 percentage so the UI can show how strong a
    /// suggestion is; <c>Source</c> records which signal produced it.
    /// </summary>
    public record TermSuggestion(
        string Term,
        int Occurrences,
        double Confidence,
        string Source);

    /// <summary>
    /// Predictive action &amp; recommendation engine.
    /// Tracks recent search terms per user and emits follow-up suggestions
    /// based on co-occurrence patterns (e.g. "Moisture Sensor B" is
    /// frequently followed by "Pump A"). Uses three in-memory frequency maps:
    /// <list type="bullet">
    ///   <item><c>_pairs</c> — transition counts termA → termB (bigram model)</item>
    ///   <item><c>_transitions</c> — total outbound transitions per term (denominator for confidence)</item>
    ///   <item><c>_frequency</c> — global term popularity used as a fallback signal</item>
    /// </list>
    /// Suggestions are ranked by descending occurrence count (frequency map + sorting).
    /// REFERENCE: Microsoft, 2026. Dictionary&lt;TKey,TValue&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2
    /// [Accessed 13 October 2026].
    /// </summary>
    public class SearchPatternTracker
    {
        private const int WindowSize = 50;

        private readonly ConcurrentDictionary<string, List<SearchEvent>> _byUser = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _pairs
            = new(); // [termA][termB] = count
        private readonly ConcurrentDictionary<string, int> _transitions = new(); // termA total
        private readonly ConcurrentDictionary<string, int> _frequency = new();  // global term count

        private long _eventCount;

        /// <summary>Total number of recorded search events.</summary>
        public long EventCount => Interlocked.Read(ref _eventCount);

        /// <summary>Number of distinct terms in the frequency map.</summary>
        public int TermCount => _frequency.Count;

        public void Record(SearchEvent ev)
        {
            var list = _byUser.GetOrAdd(ev.UserId, _ => new List<SearchEvent>());

            SearchEvent? previous;
            lock (list)
            {
                previous = list.LastOrDefault();
                list.Add(ev);
                if (list.Count > WindowSize) list.RemoveAt(0); // sliding window
            }

            Interlocked.Increment(ref _eventCount);
            _frequency.AddOrUpdate(ev.Term, 1, (_, v) => v + 1);

            if (previous is not null && previous.Term != ev.Term)
            {
                var inner = _pairs.GetOrAdd(previous.Term,
                    _ => new ConcurrentDictionary<string, int>());
                inner.AddOrUpdate(ev.Term, 1, (_, v) => v + 1);
                _transitions.AddOrUpdate(previous.Term, 1, (_, v) => v + 1);
            }
        } // (Microsoft, 2026)

        /// <summary>
        /// Returns the top N most likely follow-up terms after <paramref name="currentTerm"/>,
        /// ranked by frequency (descending) with a confidence percentage. When no
        /// co-occurrence data exists yet the engine falls back to globally popular terms
        /// so the operator always receives a useful recommendation.
        /// </summary>
        public IReadOnlyList<TermSuggestion> SuggestionsFor(string currentTerm, int topN = 3)
        {
            if (string.IsNullOrWhiteSpace(currentTerm))
                return Array.Empty<TermSuggestion>();

            // --- Signal 1: co-occurrence (sequence) analysis -------------------
            if (_pairs.TryGetValue(currentTerm, out var inner) && inner.Count > 0)
            {
                int totalTransitions = _transitions.GetValueOrDefault(
                    currentTerm, inner.Values.Sum());

                return inner
                    .OrderByDescending(kv => kv.Value)
                    .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(topN)
                    .Select(kv => new TermSuggestion(
                        Term: kv.Key,
                        Occurrences: kv.Value,
                        Confidence: totalTransitions == 0
                            ? 0
                            : Math.Round(100.0 * kv.Value / totalTransitions, 1),
                        Source: "co-occurrence"))
                    .ToList();
            }

            // --- Signal 2: popularity fallback (global frequency map) ----------
            long totalEvents = Math.Max(1, EventCount);
            return _frequency
                .Where(kv => !string.Equals(kv.Key, currentTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(topN)
                .Select(kv => new TermSuggestion(
                    Term: kv.Key,
                    Occurrences: kv.Value,
                    Confidence: Math.Round(100.0 * kv.Value / totalEvents, 1),
                    Source: "popularity"))
                .ToList();
        }

        /// <summary>The most frequently searched terms overall (frequency map + sorting).</summary>
        public IReadOnlyList<TermSuggestion> TopTerms(int topN = 5)
        {
            long totalEvents = Math.Max(1, EventCount);
            return _frequency
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(topN)
                .Select(kv => new TermSuggestion(
                    kv.Key,
                    kv.Value,
                    Math.Round(100.0 * kv.Value / totalEvents, 1),
                    "popularity"))
                .ToList();
        }

        /// <summary>Analytics feed: total events, distinct terms, known transitions.</summary>
        public object Analytics() => new
        {
            recordedEvents = EventCount,
            distinctTerms = TermCount,
            distinctTransitions = _pairs.Count,
            trackedUsers = _byUser.Count
        };
    } // (Microsoft, 2026)
}

