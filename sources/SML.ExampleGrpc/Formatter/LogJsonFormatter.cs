using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace SML.ExampleGrpc.Formatter;

public class LogJsonFormatter(string serviceName) : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var level = GetValue(logEvent, "CustomLevel");
        var threadId = GetValue(logEvent, "ThreadId") ?? 0;
        var sourceContext = GetValue(logEvent, "SourceContext")?.ToString() ?? "";
        var correlationId = GetValue(logEvent, "CorrelationId")?.ToString() ?? "";
        var machineName = GetValue(logEvent, "MachineName")?.ToString();
        var uniqueDate = GetValue(logEvent, "UniqueDate")?.ToString() ?? "";
        var methodName = GetValue(logEvent, "MethodName")?.ToString() ?? "";

        var timestampStr = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        
        var logLine = $"{timestampStr}|{serviceName}|{threadId}|{level}|{sourceContext}|{logEvent.RenderMessage()}";

        object? exceptionObj = null;
        if (logEvent.Exception != null)
        {
            exceptionObj = new
            {
                type = logEvent.Exception.GetType().FullName,
                message = logEvent.Exception.Message,
                stackTrace = logEvent.Exception.StackTrace
            };
        }

        var logObject = new
        {
            timestamp = uniqueDate,
            hostname = machineName,
            serviceName = serviceName,
            threadId = threadId,
            level = level,
            logger = sourceContext,
            className = sourceContext,
            methodName = methodName,
            correlationId = correlationId,
            message = logEvent.RenderMessage(),
            exception = exceptionObj,
            logLine = logLine
        };

        var json = JsonSerializer.Serialize(logObject, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
        });
        
        output.Write(json);
        output.Write("\n");
    }

    private object? GetValue(LogEvent logEvent, string key)
    {
        if (logEvent.Properties.TryGetValue(key, out var value))
        {
            if (value is ScalarValue scalar) return scalar.Value;
            return value.ToString();
        }
        return null;
    }
}