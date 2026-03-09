namespace SML.Display.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Polly;
using Polly.Retry;
using Settings;
using System.Diagnostics;

/// <summary>
/// Hosted service managing recurring tasks.
/// </summary>
internal abstract class RecurringTaskHostedServiceBase : BackgroundService
{
    private readonly ILogger<RecurringTaskHostedServiceBase> _logger;

    private readonly RecurringJobSettings _recurringJobSettings;
    private readonly AsyncRetryPolicy? _policy;
    private readonly string _serviceName;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private CancellationTokenSource _delayCancellationTokenSource = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">>Logger.</param>
    /// <param name="recurringJobSettings">Recurring job settings.</param>
    /// <param name="serviceName">Service name.</param>
    protected RecurringTaskHostedServiceBase(ILogger<RecurringTaskHostedServiceBase> logger,
        RecurringJobSettings recurringJobSettings,
        string serviceName)
    {
        _logger = logger;
        _logger.LogTrace("");

        _recurringJobSettings = recurringJobSettings;
        _serviceName = serviceName;
        _cancellationTokenSource = new();

        if (recurringJobSettings.FailedJobSettings is { } settings)
        {
            _policy = PollyHelper.CreateRetryPolicy(logger, settings);
        }
    }

    /// <summary>
    /// Start the service.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the hosted service is stopping.</param>
    /// <returns>Asynchronous operation task.</returns>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{HostedServiceName} starts", _serviceName);
        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stop the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>Asynchronous operation task.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("{HostedServiceName} completed shutdown in {StopWatchElapsedMilliseconds} ms", _serviceName, stopwatch.ElapsedMilliseconds);
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
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            var jobsCancellationTokenSource = cancellationTokenSource.Token;

            try
            {
                _logger.LogTrace("");

                cancellationToken.Register(() => _logger.LogInformation("Ending {HostedServiceName}", _serviceName));

                await Task.Delay(_recurringJobSettings.JobInitialDelay, jobsCancellationTokenSource);
                while (!jobsCancellationTokenSource.IsCancellationRequested)
                {
                    if (_policy is { } policy)
                    {
                        var context = new Context();
                        await policy.ExecuteAsync((context, cancellationToken) => ExecuteAction(cancellationToken), context, jobsCancellationTokenSource);
                        if (PollyHelper.GetRetries(context) is { } retries)
                        {
                            _logger.LogInformation("Recurring job of {HostedServiceName} is successful after {Retries} failed retries. " +
                                "Next execution in {JobInterval}", _serviceName, retries, _recurringJobSettings.JobInterval);
                        }
                        else
                        {
                            _logger.LogTrace("Recurring job of {HostedServiceName} is successful. " +
                                "Next execution in {JobInterval}", _serviceName, _recurringJobSettings.JobInterval);
                        }
                    }
                    else
                    {
                        try
                        {
                            await ExecuteAction(jobsCancellationTokenSource);
                            _logger.LogTrace("Recurring job of {HostedServiceName} is successful. " +
                                "Next execution in {JobInterval}", _serviceName, _recurringJobSettings.JobInterval);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Recurring job of {HostedServiceName} failed. " +
                                "Next execution in {JobInterval}", _serviceName, _recurringJobSettings.JobInterval);
                        }
                    }
                    _delayCancellationTokenSource = new CancellationTokenSource();
                    var newCancellationSourceToken = CancellationTokenSource.CreateLinkedTokenSource(jobsCancellationTokenSource, _delayCancellationTokenSource.Token);
                    await Task.Delay(_recurringJobSettings.JobInterval, newCancellationSourceToken.Token);
                }
            }
            catch (Exception e)
            {
                if (!jobsCancellationTokenSource.IsCancellationRequested)
                {
                    EnvironmentHelper.FailFast(_logger, $"{nameof(RecurringTaskHostedServiceBase)} for {_serviceName} crashed", e);
                }
            }
        }
    }

    /// <summary>
    /// Execute the recurring action that be called in loop.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the hosted service is stopping.</param>
    protected abstract Task ExecuteAction(CancellationToken cancellationToken);

    protected void CancelRecurringJobs()
    {
        _logger.LogInformation("Recurring job of {HostedServiceName} is cancelled", _serviceName);
        _cancellationTokenSource.Cancel();
    }

    protected void UpdateRecurringJobDelay(TimeSpan newInterval)
    {
        _logger.LogInformation("Recurring job of {HostedServiceName} delay is updated to {NewInterval}", _serviceName, newInterval);
        _recurringJobSettings.JobInterval = newInterval;
        CancelCurrentDelay();
    }
    protected void CancelCurrentDelay()
    {
        _logger.LogInformation("Current delay of {HostedServiceName} is cancelled", _serviceName);
        _delayCancellationTokenSource.Cancel();
    }
}
