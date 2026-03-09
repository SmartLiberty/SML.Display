namespace SML.ExampleGrpc.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Core.Interfaces.Handlers;
using Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using Settings;
using System.Threading;

/// <summary>
/// Hosted service managing the cleaning of archived storable elements.
/// </summary>
internal class CleanArchivesHostedService : RecurringTaskHostedServiceBase
{
    private readonly ILogger<CleanArchivesHostedService> _logger;

    private readonly IDatabaseAccessChecker _databaseAccessChecker;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="databaseAccessChecker">Database access checker.</param>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="settings">Recurring job settings.</param>
    public CleanArchivesHostedService(ILogger<CleanArchivesHostedService> logger,
        IDatabaseAccessChecker databaseAccessChecker,
        IServiceProvider serviceProvider, 
        IOptionsMonitor<RecurringJobSettings> settings)
        : base(logger, settings.Get(RecurringJobSettings.CleanArchives), nameof(CleanArchivesHostedService))
    {
        _logger = logger;
        _logger.LogTrace("");

        _databaseAccessChecker = databaseAccessChecker;
        _serviceProvider = serviceProvider;
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
                await _databaseAccessChecker.WaitDatabaseAccessAsync(cancellationToken);
                await base.ExecuteAsync(cancellationToken);
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(CleanArchivesHostedService)} crashed", e);
            }
        }
    }

    /// <summary>
    /// Execute the recurring action that be called in loop.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the hosted service is stopping.</param>
    protected override async Task ExecuteAction(CancellationToken cancellationToken)
    {
        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var service = serviceScope.ServiceProvider.GetRequiredService<IDeleteObsoleteArchiveService>();
            await service.DeleteObsoleteArchive();
        }
    }
}