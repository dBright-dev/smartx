using System.Collections.Concurrent;
using SmartX.API.Models;

namespace SmartX.API.DataStructures
{
    /// <summary>
    /// O(1) dictionary keyed by MAC address.
    /// Allows instant device lookup when a telemetry packet arrives.
    /// Built on <see cref="ConcurrentDictionary{TKey,TValue}"/> — the thread-safe
    /// variant of <c>Dictionary&lt;TKey,TValue&gt; registration as a singleton means
    /// every request shares this single registry.
    /// REFERENCE: Microsoft, 2026. Dictionary&lt;TKey,TValue&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2
    /// [Accessed 13 October 2026].
    /// </summary>
    public class DeviceRegistry
    {
        private readonly ConcurrentDictionary<string, SensorDevice> _devices = new();

        public void Register(SensorDevice d) => _devices[d.MacAddress] = d;

        public SensorDevice? Find(string mac)
            => _devices.TryGetValue(mac, out var d) ? d : null;

        public bool Remove(string mac) => _devices.TryRemove(mac, out _);

        public IReadOnlyCollection<SensorDevice> All() => _devices.Values.ToList();

        public int Count => _devices.Count;
    } // (Microsoft, 2026)
}
