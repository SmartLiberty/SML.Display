namespace SML.ExampleGrpc.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Hosted service managing backup registration.
/// </summary>
internal class BackupRegistrationHostedService : BackgroundService
{
	private readonly ILogger<BackupRegistrationHostedService> _logger;

	private readonly IBackupRegistrationHandler _backupRegistrationHandler;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="backupRegistrationHandler">Backup registration handler.</param>
    public BackupRegistrationHostedService(
		ILogger<BackupRegistrationHostedService> logger,
		IBackupRegistrationHandler backupRegistrationHandler)
	{
		_logger = logger ?? NullLogger<BackupRegistrationHostedService>.Instance;
		_logger.LogTrace("Begin");

		_backupRegistrationHandler = backupRegistrationHandler;
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
                await _backupRegistrationHandler.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(BackupRegistrationHostedService)} crashed", e);
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
		_backupRegistrationHandler.Stop();
		await base.StopAsync(cancellationToken);
	}
}