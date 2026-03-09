namespace SML.ExampleGrpc.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Data.Settings;
using Core.Helpers;
using Core.Interfaces.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SML.Agent.Shared.Dtos;

/// <summary>
/// Hosted service managing health check.
/// </summary>
internal class HealthCheckHostedService : BackgroundService
{
    private readonly ILogger<HealthCheckHostedService> _logger;

    private readonly IGenericProducer<HealthCheckEvent> _healthCheckEventsProducer;
    private readonly GeneralSettings _generalSettings;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="healthCheckEventsProducer">Health check events producer.</param>
    /// <param name="generalSettings">General settings.</param>
    public HealthCheckHostedService(ILogger<HealthCheckHostedService> logger,
        IGenericProducer<HealthCheckEvent> healthCheckEventsProducer,
        IOptions<GeneralSettings> generalSettings)
    {
        _logger = logger;
        _logger.LogTrace("Begin");

        _healthCheckEventsProducer = healthCheckEventsProducer;
        _generalSettings = generalSettings.Value;
    }

    /// <summary>
    /// Called when the hosted service starts.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the hosted service is stopping.</param>
    /// <returns>Asynchronous operation task.</returns>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            try
            {
                _logger.LogDebug("Health check events producer starting...");
                await _healthCheckEventsProducer.StartAsync(cancellationToken);
                var healthCheckEvent = new HealthCheckEvent(_generalSettings.ServiceName, _generalSettings.HealthCheckUrl);
                _logger.LogInformation("Publish Health check event: {@HealthCheckEvent}", healthCheckEvent);
                await _healthCheckEventsProducer.PublishAsync(healthCheckEvent, cancellationToken);
                _logger.LogDebug("Health check event published");
                _healthCheckEventsProducer.Stop();
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(HealthCheckHostedService)} crashed", e);
            }
        }
    }
}