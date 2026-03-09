namespace SML.Display.Core.HealthChecks;

using Core.Interfaces.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

public class StartupHealthChecks : IStartupHealthChecks
{
    private bool _serviceWasStarted;

    private readonly ILogger<StartupHealthChecks> _logger;

    public StartupHealthChecks(ILogger<StartupHealthChecks> logger)
    {
        _serviceWasStarted = false; 
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Service 'was started' current state : {ServiceWasStarted}", _serviceWasStarted);
        return Task.FromResult(_serviceWasStarted ? HealthCheckResult.Healthy("Service started") : HealthCheckResult.Unhealthy("Service not started"));
    }

    public void ServiceStarted()
    {
        _serviceWasStarted = true;
        _logger.LogInformation("Service has been started");
    } 
}
