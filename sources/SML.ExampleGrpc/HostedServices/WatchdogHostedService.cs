namespace SML.ExampleGrpc.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Data.Settings;
using Core.Helpers;
using Core.Watchdog;
using Core.Watchdog.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Settings;
using System.Threading;

/// <summary>
/// Hosted service managing watchdog.
/// </summary>
internal class WatchdogHostedService : RecurringTaskHostedServiceBase
{
    private const int ServiceEventId = 1;
    private const string EventDisplayName = "IsAlive";

    private readonly ILogger<WatchdogHostedService> _logger;

	private readonly IWatchdogClientService _watchdogClientService;
    private readonly WatchdogClientSettings _watchdogClientSettings;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="watchdogClientService">Watchdog client service.</param>
    /// <param name="watchdogClientSettings">Watchdog client settings.</param>
    /// <param name="settings">Recurring job settings.</param>
    public WatchdogHostedService(ILogger<WatchdogHostedService> logger,
		IWatchdogClientService watchdogClientService,
        IOptions<WatchdogClientSettings> watchdogClientSettings,
		IOptionsMonitor<RecurringJobSettings> settings)
        : base(logger, settings.Get(RecurringJobSettings.Watchdog), nameof(WatchdogHostedService))
    {
        _logger = logger;
        _logger.LogTrace("");

		_watchdogClientService = watchdogClientService;
        _watchdogClientSettings = watchdogClientSettings.Value;
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
                await _watchdogClientService.RegisterServiceAsync(cancellationToken, new DtoEventRegistration
                {
                    Id = ServiceEventId,
                    DisplayName = EventDisplayName,
                    Period = _watchdogClientSettings.Period,
                    GracePeriod = _watchdogClientSettings.GracePeriod,
                });
                await base.ExecuteAsync(cancellationToken);
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(WatchdogHostedService)} crashed", e);
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
            await _watchdogClientService.EventTriggeredAsync(cancellationToken, ServiceEventId);
        }
    }
}