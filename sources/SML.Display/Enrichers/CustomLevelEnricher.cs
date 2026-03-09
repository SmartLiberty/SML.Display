using Serilog.Core;
using Serilog.Events;

namespace SML.Display.Enrichers;

/// <summary>
/// Enricher that map the log level to SML custom level strings.
/// </summary>
public class CustomLevelEnricher : ILogEventEnricher
{
    public const string PropertyName = "CustomLevel";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var customLevel = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VERBOSE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => "Unknown"
        };

        var property = propertyFactory.CreateProperty(PropertyName, customLevel);
        logEvent.AddPropertyIfAbsent(property);
    }
}
