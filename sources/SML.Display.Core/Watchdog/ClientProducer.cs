namespace SML.Display.Core.Watchdog;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Threading;

/// <summary>
/// Generic RabbitMQ producer.
/// </summary>
/// <typeparam name="T">Type of the DTO event to publish.</typeparam>
[Obsolete("To be deleted after watchdog refactoring."
    + "Please do not use for anything other than obsolete watchdog.")]
public class ClientProducer<T> : RabbitMqBase, IClientProducer<T> where T : class
{
    private readonly ILogger<ClientProducer<T>> _logger;

    private readonly object _locker;

    public ClientProducer(
        ILogger<ClientProducer<T>> logger,
        IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings settings,
        ClientExchangeSettings exchangeSettings)
        : base(channelFactory, settings, exchangeSettings.Name)
    {
        _logger = logger;
        _logger.LogTrace("");

        _locker = new();
    }

    /// <summary>
    /// Initialize RabbitMQ communication.
    /// </summary>
    protected override void Initialize(IModel channel)
        => channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, true);

    /// <summary>
    /// End RabbitMQ communication.
    /// </summary>
    protected override void End() { }

    /// <summary>
    /// Publish a DTO event.
    /// </summary>
    /// <param name="dtoEvent">DTO event to publish.</param>
    /// <returns>True wether the DTO event has been published, false otherwise.</returns>
    public bool Publish(T dtoEvent)
    {
        try
        {
            if (Channel == null)
            {
                _logger.LogWarning("Not yet started or stopped (no channel)");
                return false;
            }
            PublishEvent(dtoEvent);
            return true;
        }
        catch (Exception e)
        {
            if (Channel == null)
            {
                _logger.LogWarning("Not yet started or stopped (no channel)");
            }
            else
            {
                _logger.LogError(e, "");
            }
            return false;
        }
    }

    /// <summary>
    /// Publish a DTO event.
    /// </summary>
    /// <param name="dtoEvent">DTO event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PublishAsync(T dtoEvent, CancellationToken cancellationToken)
    {
        var attempts = Settings.MaxAttempts;
        var backoff = Settings.InitialBackoff;
        var logError = true;
        var logUnexpectedError = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (Channel == null)
                {
                    _logger.LogWarning("Not yet started or stopped (no channel)");
                    return;
                }
                PublishEvent(dtoEvent);
                return;
            }
            catch (AlreadyClosedException)
            {
                if (logError)
                {
                    _logger.LogError("RabbitMQ connection closed");
                    logError = false;
                }
            }
            catch (Exception e)
            {
                if (Channel == null)
                {
                    _logger.LogWarning("Not yet started or stopped (no channel)");
                    return;
                }
                if (logUnexpectedError)
                {
                    _logger.LogError(e, "Unexpected error occurs");
                    logUnexpectedError = false;
                }
            }
            _logger.LogTrace("{RabbitMqChannelConnectionRemainingAttempts} remaining attempts, following backoff: {RabbitMqChannelConnectionBackoff}", attempts, backoff.ToString(@"hh\:mm\:ss"));
            if (--attempts <= 0)
            {
                _logger.LogWarning("No more attempts for this unpublished event: {@UnpublishedRabbitMqEvent}", dtoEvent);
                return;
            }
            await Task.Delay(backoff, cancellationToken);
            backoff = ChannelFactory.Increase(backoff);
        }
        _logger.LogInformation("Cancelled");
    }

    private void PublishEvent(T dtoEvent)
    {
        // Lock mandatory to prevent concurrency errors
        lock (_locker)
        {
            Channel!.BasicPublish(ExchangeName,
                routingKey: "",
                basicProperties: null,
                body: MessagePackSerializer.Serialize(dtoEvent));
        }
    }
}
