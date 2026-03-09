namespace SML.Display.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Core.Interfaces.Handlers;

/// <summary>
/// Hosted service managing service version information.
/// </summary>
internal class ServiceVersionInfoHostedService : BackgroundService
{
    private readonly ILogger<ServiceVersionInfoHostedService> _logger;
    private readonly IServiceVersionInfoHandler _serviceVersionInfoHandler;

    /// <summary>
    /// Constructor.
    /// </summary>
	/// <param name="logger">Logger.</param>
    /// <param name="serviceVersionInfoHandler">Service version info handler.</param>
	public ServiceVersionInfoHostedService(ILogger<ServiceVersionInfoHostedService> logger, 
        IServiceVersionInfoHandler serviceVersionInfoHandler)
    {
        _logger = logger;
        _logger.LogTrace("Begin");
        _serviceVersionInfoHandler = serviceVersionInfoHandler;
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
                _logger.LogTrace("");
                await _serviceVersionInfoHandler.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(ServiceVersionInfoHostedService)} crashed", e);
            }
        }
    }

    /// <summary>
    /// Stop the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>Asynchronous operation task.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _serviceVersionInfoHandler.Stop();
        await base.StopAsync(cancellationToken);
    }
}
