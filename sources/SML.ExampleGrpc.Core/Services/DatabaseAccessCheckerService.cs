namespace SML.ExampleGrpc.Core.Services;

using Core.Data.Settings;
using Core.Interfaces.Handlers;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Database access checker service.
/// </summary>
public class DatabaseAccessCheckerService : IDatabaseAccessCheckerService
{
    private readonly ILogger<DatabaseAccessCheckerService> _logger;
    private readonly IDatabaseAccessChecker _databaseAccessChecker;
    private readonly IProcessKiller _processKiller;
    private readonly PostgresReconnectionSettings _postgresReconnectionSettings;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="databaseAccessChecker">Database access checker handler.</param>
    /// <param name="processKiller">Process killer.</param>
    /// <param name="postgresReconnectionSettings">Postgres Reconnection settings.</param>
    public DatabaseAccessCheckerService(ILogger<DatabaseAccessCheckerService> logger,
        IDatabaseAccessChecker databaseAccessChecker,
        IProcessKiller processKiller,
        IOptions<PostgresReconnectionSettings> postgresReconnectionSettings)
    {
        _logger = logger;
        _logger.LogTrace("Begin");

        _databaseAccessChecker = databaseAccessChecker;
        _processKiller = processKiller;
        _postgresReconnectionSettings = postgresReconnectionSettings.Value;
    }

    public async Task CheckDatabaseAccessibleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Begin");

        var retries = 0;
        do
        {
            if (await _databaseAccessChecker.IsDatabaseAccessible(cancellationToken))
            {
                if (retries > 0)
                {
                    _logger.LogInformation("Reconnected to Postgres after {DatabaseAccessibleAfterRetriesCount} retries! ", retries);
                }
                return;
            }

            retries++;
            if (retries > _postgresReconnectionSettings.NumberOfRetriesBeforeKill)
            {
                _logger.LogWarning("Connection to postgres failed after retrying connection {DatabaseConnectionFailedAfterMaxRetryCount} times! Self killing service (suicide)...", _postgresReconnectionSettings.NumberOfRetriesBeforeKill);
                _processKiller.Kill("Maximum attempts have been reached for Postgres connection retries!");
            }
            _logger.LogWarning("Connection to postgres failed, retrying in {DatabaseConnectionFailedRetryInterval} seconds...", _postgresReconnectionSettings.RetryInterval.TotalSeconds);
            await Task.Delay(_postgresReconnectionSettings.RetryInterval, cancellationToken);
        }
        while (!cancellationToken.IsCancellationRequested);
    }
}