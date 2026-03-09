namespace SML.ExampleGrpc.Core.Correlations.HttpMiddleware;

using Microsoft.AspNetCore.Builder;

public static class AddCorrelationMiddleware
{
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder.UseMiddleware<CorrelationMiddleware>();
    }
}
