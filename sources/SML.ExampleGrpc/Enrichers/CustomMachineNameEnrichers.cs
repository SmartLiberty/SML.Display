using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SML.ExampleGrpc.Enrichers;

/// <summary>
/// Enricher that adds the machine name from various environment variables because the Serilog one crop the size.
/// </summary>
public class CustomMachineNameEnrichers : ILogEventEnricher
{
    public const string PropertyName = "MachineName";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var machineName = TryGetValue(() => Environment.GetEnvironmentVariable("HOSTNAME"))
                          ?? TryGetValue(() => Environment.GetEnvironmentVariable("COMPUTERNAME"))
                          ?? TryGetValue(() => Environment.GetEnvironmentVariable("MACHINENAME"))
                          ?? TryGetValue(() => Environment.MachineName)
                          ?? string.Empty;
        
        var property = propertyFactory.CreateProperty(PropertyName, machineName);
        logEvent.AddPropertyIfAbsent(property);
    }

    private static string? TryGetValue(Func<string?> getter)
    {
        try
        {
            var result = getter()?.Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to get environment variable value");
            return null;
        }
    }
}