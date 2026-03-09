namespace SML.Display.Core.Correlations.HttpMiddleware;

using Core.Correlations.ThreadStorages;
using Microsoft.AspNetCore.Http;

public class CorrelationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdThreadStorageHolder.CorrelationIdHeader, out var correlationIdHeader)
            && correlationIdHeader.FirstOrDefault() is { } correlationId)
        {
            CorrelationIdThreadStorageHolder.CorrelationId = correlationId;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdThreadStorageHolder.CorrelationIdHeader] = new[] { CorrelationIdThreadStorageHolder.CorrelationId };

            return Task.CompletedTask;
        });

        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            await _next(context);
        }
    }
}
