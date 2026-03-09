namespace SML.Display.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Hosted service managing database access.
/// </summary>
internal class DatabaseAccessCheckerHostedService : BackgroundService
{
    private readonly ILogger<DatabaseAccessCheckerHostedService> _logger;

    private readonly IDatabaseAccessCheckerService _databaseAccessCheckerService;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="databaseAccessCheckerService">Database access checker service.</param>
    public DatabaseAccessCheckerHostedService(ILogger<DatabaseAccessCheckerHostedService>? logger,
        IDatabaseAccessCheckerService databaseAccessCheckerService)
    {
        _logger = logger ?? NullLogger<DatabaseAccessCheckerHostedService>.Instance;
        _logger.LogTrace("Begin");

        _databaseAccessCheckerService = databaseAccessCheckerService;
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
                await _databaseAccessCheckerService.CheckDatabaseAccessibleAsync(cancellationToken);
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(DatabaseAccessCheckerHostedService)} crashed", e);
            }
        }
    }
}
