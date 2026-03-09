using Serilog.Core;
using Serilog.Events;

namespace SML.Display.Enrichers;

/// <summary>
/// Enriches log events with a unique date property.
/// </summary>
public class UniqueDateEnrichers : ILogEventEnricher
{
    public const string PropertyName = "UniqueDate";
    private const string CounterFormat = "D6";
    private const int Limit = 5;

    private static readonly object _lock = new();

    // key = yyyy-MM-dd HH:mm:ss
    private static readonly Dictionary<string, CounterEntry> _counters = new();
    private static readonly LinkedList<string> _order = new();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var utcTimestamp = logEvent.Timestamp.UtcDateTime;
        
        string fullDate = utcTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string secondKey = utcTimestamp.ToString("yyyy-MM-dd HH:mm:ss");

        long counter;

        lock (_lock)
        {
            if (_counters.TryGetValue(secondKey, out var entry))
            {
                counter = ++entry.Counter;
            }
            else
            {
                var node = _order.AddFirst(secondKey);

                entry = new CounterEntry
                {
                    Counter = 0,
                    Node = node
                };

                _counters[secondKey] = entry;
                counter = 0;

                if (_counters.Count > Limit)
                {
                    var toRemove = _order.Last!;
                    _order.RemoveLast();
                    _counters.Remove(toRemove.Value);
                }
            }
        }

        string result = $"{fullDate}{counter.ToString(CounterFormat)}";
        
        var property = propertyFactory.CreateProperty(PropertyName, result);
        logEvent.AddPropertyIfAbsent(property);
    }

    private sealed class CounterEntry
    {
        public long Counter;
        public LinkedListNode<string> Node = null!;
    }
}
