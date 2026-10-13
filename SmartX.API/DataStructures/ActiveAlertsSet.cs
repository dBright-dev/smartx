namespace SmartX.API.DataStructures
{
    /// <summary>
    /// HashSet stores only unique alert keys — prevents duplicate disconnect
    /// alerts for the same sensor arriving repeatedly.
    /// Keys are formatted as "macAddress|errorCode" so the SAME error code from
    /// the SAME device is only processed once until it is explicitly cleared.
    /// REFERENCE: Microsoft, 2026. HashSet&lt;T&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1
    /// [Accessed 13 October 2026].
    /// </summary>
    public class ActiveAlertsSet
    {
        private readonly HashSet<string> _active = new();
        private readonly object _lock = new();

        /// <returns>true if the alert was newly added (not a duplicate).</returns>
        public bool TryAdd(string macAddress, string errorCode)
        {
            lock (_lock)
                return _active.Add($"{macAddress}|{errorCode}");
        }

        public bool Contains(string macAddress, string errorCode)
        {
            lock (_lock)
                return _active.Contains($"{macAddress}|{errorCode}");
        }

        public bool Clear(string macAddress, string errorCode)
        {
            lock (_lock)
                return _active.Remove($"{macAddress}|{errorCode}");
        }

        public int Count { get { lock (_lock) return _active.Count; } }

        public IEnumerable<string> All() { lock (_lock) return _active.ToList(); }
    } // (Microsoft, 2026)
}
