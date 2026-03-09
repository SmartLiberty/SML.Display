namespace SML.ExampleGrpc.Core.Events;

using Core.Correlations.ThreadStorages;
using Core.Data.Settings;
using Core.Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

/// <summary>
/// Generic RabbitMQ consumer.
/// </summary>
/// <typeparam name="T">Type of the event to consume.</typeparam>
public class GenericConsumer<T> : RabbitMqBase, IGenericConsumer<T> where T : class
{
    private readonly ILogger<GenericConsumer<T>> _logger;

    private readonly string[] _routingKeys;
    private readonly bool _durable;

    /// <summary>
    /// Event raised when an event is consumed.
    /// </summary>
    public event Action<T>? ConsumedEvent;

    private EventingBasicConsumer? _consumer;
    private readonly string _queue;

    public GenericConsumer(ILogger<GenericConsumer<T>> logger,
        IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings settings,
        string routingKey,
        bool durable = false,
        QueueSuffix? queueSuffix = null)
        : this(logger, channelFactory, settings, [routingKey], durable, queueSuffix)
    {
    }

    public GenericConsumer(ILogger<GenericConsumer<T>> logger,
        IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings settings,
        bool durable = false,
        QueueSuffix? queueSuffix = null)
        : this(logger, channelFactory, settings, ["#"], durable, queueSuffix)
    {
    }

    public GenericConsumer(ILogger<GenericConsumer<T>> logger,
        IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings settings,
        string[] routingKeys,
        bool durable = false,
        QueueSuffix? queueSuffix = null)
        : base(channelFactory, settings, typeof(T).FullName!)
    {
        _logger = logger;
        _logger.LogTrace("");

        _routingKeys = routingKeys;
        _durable = durable;
        _queue = $"{ExchangeName}-{GetType().Namespace}.GenericConsumer";
        if (queueSuffix != null)
        {
            _queue = queueSuffix.AddSuffix(_queue);
        }
    }

    /// <summary>
    /// Initialize RabbitMQ communication.
    /// </summary>
    protected override void Initialize(IModel channel)
    {
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
        var queue = channel.QueueDeclare(
            queue: _queue,
            durable: _durable,
            exclusive: false,
            autoDelete: !_durable,
            arguments: null).QueueName;

        foreach (var routingKey in _routingKeys)
        {
            channel.QueueBind(queue, ExchangeName, routingKey: routingKey);
        }

        _consumer = new EventingBasicConsumer(channel);
        _consumer.Received += OnReceived;

        channel.BasicConsume(queue, autoAck: false, _consumer);
    }

    /// <summary>
    /// End RabbitMQ communication.
    /// </summary>
    protected override void End()
    {
        if (_consumer != null)
        {
            _consumer.Received -= OnReceived;
        }
        _consumer = null;
    }

    private void OnReceived(object? sender, BasicDeliverEventArgs arguments)
    {
        if (arguments.BasicProperties?.Headers?[CorrelationIdThreadStorageHolder.CorrelationIdHeader] is byte[] id)
        {
            CorrelationIdThreadStorageHolder.CorrelationId = Encoding.UTF8.GetString(id);
        }

        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            try
            {
                RaiseEvent(MessagePackSerializer.Deserialize<T>(arguments.Body));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "");
            }
            Channel?.BasicAck(arguments.DeliveryTag, false);
        }
    }

    private void RaiseEvent(T rmqEvent)
    {
        try
        {
            ConsumedEvent?.Invoke(rmqEvent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "");
        }
    }
}