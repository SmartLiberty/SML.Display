using System.Diagnostics;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace SML.ExampleGrpc.Enrichers;

/// <summary>
/// Enricher that add the method name property via stack trace.
/// </summary>
public class MethodNameEnrichers(string serviceName) : ILogEventEnricher
{
    public const string PropertyName = "MethodName";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var stackTrace = new StackTrace(1, false); 
        var frames = stackTrace.GetFrames();
    
        if (frames == null) return;

        for (var i = 0; i < frames.Length; i++)
        {
            var method = frames[i].GetMethod();
            if (method == null) continue;

            var declaringType = method.DeclaringType;
            if (declaringType == null) continue;

            var typeName = declaringType.FullName;

            if (typeName.StartsWith("Serilog.", StringComparison.OrdinalIgnoreCase)) 
                continue;

            if (typeName.Contains("Enrichers") || typeName.Contains("Logging"))
                continue;

            var methodName = GetCleanMethodName(method);
        
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, $"{methodName}"));
            return; 
        }
    }

    private static string GetCleanMethodName(MethodBase method)
    {
        if (method.Name == "MoveNext" && method.DeclaringType?.Name.Contains("<") == true)
        {
            var name = method.DeclaringType.Name;
            var start = name.IndexOf('<') + 1;
            var end = name.IndexOf('>');
            if (start > 0 && end > start) return name.Substring(start, end - start);
        }
        return method.Name;
    }
}