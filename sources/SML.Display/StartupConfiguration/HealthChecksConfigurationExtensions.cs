namespace SML.Display.StartupConfiguration;

using Core.Interfaces.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

public static class HealthChecksConfigurationExtensions
{
    private const string TagHealtz = "healtz";
    private const string TagReadiness = "readiness";
    
    public const string EndpointHealthCheck = "/healthcheck";
    
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddGrpcHealthChecks(o =>
            {
                o.Services.MapService("healtz", r => r.Tags.Contains(TagHealtz));
            })
            .AddCheck<IStartupHealthChecks>("check", null, new[] { TagHealtz, TagReadiness });
        
        return services;
    }

    public static void MapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcHealthChecksService();
        
        endpoints.MapHealthChecks(EndpointHealthCheck, new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
    }
}
