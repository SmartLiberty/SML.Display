namespace SML.Display.Core.Events;

using Core.Correlations.ThreadStorages;
using Core.Data.Settings;
using Core.Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Threading;

/// <summary>
/// Generic RabbitMQ producer.
/// </summary>
/// <typeparam name="T">Type of the event to publish.</typeparam>
public class GenericProducer<T> : RabbitMqBase, IGenericProducer<T> where T: class
{
    private readonly ILogger<GenericProducer<T>> _logger;

    private IBasicProperties? _basicProperties;

    private readonly Func<T, string> _routingKeyFunction;

    private CancellationTokenSource _cancellationTokenSource;

    private readonly object _locker;

    public GenericProducer(
        ILogger<GenericProducer<T>> logger,
		IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings settings,
        Func<T, string>? routingKeyFunction = null)
        : base(channelFactory, settings, typeof(T).FullName!)
    {
        _logger = logger;
        _logger.LogTrace("");

        _routingKeyFunction = routingKeyFunction ?? ((T _) => "");

		_cancellationTokenSource = new();

        _locker = new();
    }

	/// <summary>
	/// Initialize RabbitMQ communication.
	/// </summary>
	protected override void Initialize(IModel channel)
    {
		if (_cancellationTokenSource.IsCancellationRequested)
		{
			_cancellationTokenSource = new();
		}
		channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
        _basicProperties = channel.CreateBasicProperties();
        _basicProperties.Persistent = true;
    }

	/// <summary>
	/// End RabbitMQ communication.
	/// </summary>
	protected override void End()
    {
		_cancellationTokenSource.Cancel();
	}

	/// <summary>
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	/// <returns>True whether the event has been published, false otherwise.</returns>
	public bool Publish(T rmqEvent)
    {
        try
		{
			if (Channel == null)
			{
				_logger.LogWarning("Not yet started or stopped (no channel)");
				return false;
			}
			PublishEvent(rmqEvent);
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
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	public async Task PublishAsync(T rmqEvent)
		=> await PublishAsync(rmqEvent, _cancellationTokenSource.Token);

	/// <summary>
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task PublishAsync(T rmqEvent, CancellationToken cancellationToken)
	{
        var attempts = Settings.MaxAttempts;
        var backoff = Settings.InitialBackoff;
        var logConnectionClosedError = true;
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
				PublishEvent(rmqEvent);
				return;
			}
			catch (AlreadyClosedException)
			{
				if (logConnectionClosedError)
				{
					logConnectionClosedError = false;
					_logger.LogError("RabbitMQ connection closed");
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
					logUnexpectedError = false;
					_logger.LogError(e, "Unexpected error occurs");
				}
			}
			_logger.LogTrace("{RabbitMqChannelConnectionRemainingAttempts} remaining attempts, following backoff: {RabbitMqChannelConnectionBackoff}", attempts, backoff.ToString(@"hh\:mm\:ss"));
			if (--attempts <= 0)
			{
				_logger.LogWarning("No more attempts for this unpublished event: {@UnpublishedRabbitMqEvent}", rmqEvent);
				return;
			}
			await Task.Delay(backoff, cancellationToken);
            backoff = ChannelFactory.Increase(backoff);
        }
        _logger.LogInformation("Cancelled");
    }

	private void PublishEvent(T rmqEvent)
	{
        // Lock mandatory to prevent concurrency errors
        lock (_locker)
        {
            _basicProperties!.Headers = new Dictionary<string, object>() { { CorrelationIdThreadStorageHolder.CorrelationIdHeader, CorrelationIdThreadStorageHolder.CorrelationId } };
            Channel!.BasicPublish(ExchangeName,
                routingKey: _routingKeyFunction(rmqEvent),
                basicProperties: _basicProperties,
                body: MessagePackSerializer.Serialize(rmqEvent));
        }
    }
}
