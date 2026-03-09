namespace SML.ExampleGrpc.HostedServices;

using Core.Correlations.ThreadStorages;
using Core.Helpers;
using Microsoft.Extensions.Options;
using Settings;

internal class LoggOneLineHostedService : RecurringTaskHostedServiceBase
{
    private readonly ILogger<LoggOneLineHostedService> _logger;

    public LoggOneLineHostedService(
        ILogger<LoggOneLineHostedService> logger,
        IOptionsMonitor<RecurringJobSettings> settings
    ) : base(
        logger,
        settings.Get(RecurringJobSettings.LoggOneLine),
        nameof(LoggOneLineHostedService)
    )
    {
        _logger = logger;
        _logger.LogTrace("LoggOneLineHostedService initialized.");
    }

    protected override async Task ExecuteAction(CancellationToken cancellationToken)
    {
        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            try
            {
                _logger.LogInformation("Daily log entry");

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                EnvironmentHelper.FailFast(_logger, $"{nameof(LoggOneLineHostedService)} crashed", e);
            }
        }
    }
}
