using Serilog;
using Serilog.Context;

namespace SML.ExampleGrpc.Core.Correlations.ThreadStorages;

public static class CorrelationIdThreadStorageHolder
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdPropertyName = "CorrelationId";

    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string CorrelationId
    {
        get => _correlationId.Value ??= Guid.NewGuid().ToString("D");
        set => _correlationId.Value = value;
    }

    public static IDisposable PushCorrelationContext()
    {
        return LogContext.PushProperty(CorrelationIdPropertyName, CorrelationId);
    }
}
