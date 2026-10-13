using SmartX.API.Models;

namespace SmartX.API.DataStructures
{
    /// <summary>
    /// LIFO stack that stores manual override commands so operators can "undo"
    /// the last issued action (e.g. reverting a "shut down all valves").
    /// REFERENCE: Microsoft, 2026. Stack&lt;T&gt; Class.
    /// [online] Available at:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1
    /// [Accessed 13 October 2026].
    /// </summary>
    public class CommandHistory
    {
        private readonly Stack<ManualCommand> _stack = new();
        private readonly object _lock = new();

        public void Push(ManualCommand cmd)
        {
            lock (_lock) _stack.Push(cmd);
        }

        public ManualCommand? Pop()
        {
            lock (_lock) return _stack.Count > 0 ? _stack.Pop() : null;
        }

        public ManualCommand? Peek()
        {
            lock (_lock) return _stack.Count > 0 ? _stack.Peek() : null;
        }

        public int Count { get { lock (_lock) return _stack.Count; } }

        /// <summary>Newest command first (the order an operator sees in the UI).</summary>
        public IEnumerable<ManualCommand> Snapshot()
        {
            lock (_lock) return _stack.ToArray();
        }
    } // (Microsoft, 2026)
}
