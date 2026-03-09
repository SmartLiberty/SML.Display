namespace SML.Display.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Core.Interfaces.Handlers;
using Core.Interfaces.HealthChecks;
using Core.Interfaces.Main;

/// <summary>
/// Hosted service managing main features of the service.
/// </summary>
internal class MainHostedService : BackgroundService
{
    private readonly ILogger<MainHostedService> _logger;

    private readonly IMainController _mainController;
    private readonly IStartupHealthChecks _startupHealthChecks;
    private readonly IDatabaseAccessChecker _databaseAccessChecker;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="mainController">Main controller of the service.</param>
    /// <param name="startupHealthChecks">Startup health checks.</param>
    /// <param name="databaseAccessChecker">Database access checker.</param>
    public MainHostedService(ILogger<MainHostedService> logger,
        IMainController mainController,
        IStartupHealthChecks startupHealthChecks,
        IDatabaseAccessChecker databaseAccessChecker)
    {
        _logger = logger;
        _logger.LogTrace("Begin");

        _mainController = mainController;

        _startupHealthChecks = startupHealthChecks;
        _databaseAccessChecker = databaseAccessChecker;
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
                _logger.LogInformation("Begin");

                await _databaseAccessChecker.WaitDatabaseAccessAsync(cancellationToken);
                await _mainController.StartAsync(cancellationToken);
                _logger.LogInformation("Started");

                _startupHealthChecks.ServiceStarted();

                _logger.LogInformation("End");
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(MainHostedService)} crashed", e);
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
        _logger.LogInformation("Begin");

        _mainController.Stop();
        await base.StopAsync(cancellationToken);

        _logger.LogInformation("End");
    }
}
