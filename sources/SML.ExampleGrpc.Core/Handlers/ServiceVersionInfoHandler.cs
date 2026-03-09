namespace SML.ExampleGrpc.Core.Handlers;

using Core.Interfaces.Events;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using SML.VersionInfos.Events;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Handling service version information.
/// </summary>
public class ServiceVersionInfoHandler : IServiceVersionInfoHandler
{
    private readonly ILogger<ServiceVersionInfoHandler> _logger;

    private readonly IGenericProducer<VersionEvent> _versionEventProducer;
    private readonly IGenericConsumer<VersionRequestEvent> _versionRequestEventConsumer;

    private readonly VersionEvent _versionEvent;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="logger">The logger used for logging.</param>
    /// <param name="assembly">The assembly containing version information.</param>
    /// <param name="versionEventProducer">The producer for VersionEvent.</param>
    /// <param name="versionRequestEventConsumer">The consumer for VersionRequestEvent.</param>
    public ServiceVersionInfoHandler(
        ILogger<ServiceVersionInfoHandler> logger,
        Assembly assembly,
        IGenericProducer<VersionEvent> versionEventProducer,
        IGenericConsumer<VersionRequestEvent> versionRequestEventConsumer)
    {
        _logger = logger;
        _logger.LogTrace("");

        _versionEventProducer = versionEventProducer;
        _versionRequestEventConsumer = versionRequestEventConsumer;

		_versionEvent = new VersionEvent
        {
            ServiceName = assembly.GetName().Name!,
            Version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion!
		};
	}

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
		_logger.LogInformation("{ServiceVersion}", _versionEvent.Version);

		await _versionEventProducer.StartAsync(cancellationToken);

        _versionRequestEventConsumer.ConsumedEvent += OnVersionRequestEvent;
        await _versionRequestEventConsumer.StartAsync(cancellationToken);

        await _versionEventProducer.PublishAsync(_versionEvent, cancellationToken);
	}

    /// <inheritdoc/>
    public void Stop()
    {
		_logger.LogTrace("");

		_versionRequestEventConsumer.Stop();
        _versionRequestEventConsumer.ConsumedEvent -= OnVersionRequestEvent;

        _versionEventProducer.Stop();
    }

    private void OnVersionRequestEvent(VersionRequestEvent _)
        => _versionEventProducer.PublishAsync(_versionEvent).Wait();
}
