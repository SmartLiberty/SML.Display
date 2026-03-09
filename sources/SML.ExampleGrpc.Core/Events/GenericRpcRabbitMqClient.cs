namespace SML.ExampleGrpc.Core.Events;

using Core.Data.Settings;
using Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SML.Shared.RabbitMq.Rpc;
using System.Collections.Concurrent;

/// <summary>
/// Generic RPC RabbitMQ client.
/// </summary>
/// <typeparam name="T">Request type.</typeparam>
/// <typeparam name="K">Response type.</typeparam>
public class GenericRpcRabbitMqClient<T, K> : RabbitMqBase, IGenericRpcRabbitMqClient<T, K>
    where T : class
    where K : class?
{
    private const int _defaultTimeoutInMilliseconds = 10_000;
	private readonly int _responseTimeoutInMillisecond;
    private readonly ILogger<GenericRpcRabbitMqClient<T, K>> _logger;

    private readonly object _locker;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="channelFactory">RabbitMQ channel factory.</param>
    /// <param name="rabbitMqSettings">RabbitMQ settings.</param>
    /// <param name="responseTimeoutInMilliseconds">Response timeout in milliseconds.</param>
	public GenericRpcRabbitMqClient(
        ILogger<GenericRpcRabbitMqClient<T, K>> logger,
        IRabbitMqChannelFactory channelFactory,
        RabbitMqSettings rabbitMqSettings,
        int responseTimeoutInMilliseconds = _defaultTimeoutInMilliseconds)
        : base(channelFactory, rabbitMqSettings, $"{typeof(T).FullName}-rpc")
    {
        _logger = logger;
        _logger.LogTrace("Begin");
        _responseTimeoutInMillisecond = responseTimeoutInMilliseconds;
        _locker = new();
    }

    /// <summary>
    /// Initialize RabbitMQ communication.
    /// </summary>
    protected override void Initialize(IModel channel)
    {
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, true);
    }

    /// <summary>
    /// End RabbitMQ communication.
    /// </summary>
    protected override void End()
    {
    }

    /// <summary>
    /// Publish a request.
    /// </summary>
    /// <param name="request">Request to publish by event.</param>
    /// <returns>Response.</returns>
    public K? Publish(T request)
    {
        try
        {
            // Lock mandatory to prevent concurrency errors
            lock (_locker)
            {
                if (Channel == null)
                {
                    return default;
                }

                var (respQueue, props) = ConfigureResponseQueue();

                Channel.BasicPublish(ExchangeName,
                    routingKey: "",
                    basicProperties: props,
                    body: MessagePackSerializer.Serialize(request));
                if (respQueue.TryTake(out var responseBody, _responseTimeoutInMillisecond))
                {
                    var response = ProcessRpcResponse(responseBody);
                    if (response.StatusCode == (int)StatusCode.OK)
                    {
                        return response.Message;
                    }
                    throw new RabbitMqRpcException(response.StatusCode, response.ErrorMessage);
                }
                else
                {
                    throw new RabbitMqRpcException((int)StatusCode.RequestTimeout, $"No response after {_responseTimeoutInMillisecond} milliseconds");
                }

            }
        }
        catch (RabbitMqRpcException) { throw; }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed trying to publish message");
            throw new RabbitMqRpcException(StatusCode.Internal, e.Message);
        }
    }

    private (BlockingCollection<byte[]>, IBasicProperties) ConfigureResponseQueue()
    {
        _logger.LogTrace("Begin");

        var replyQueueName = Channel.QueueDeclare().QueueName;
        var consumer = new EventingBasicConsumer(Channel);

        var props = Channel!.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;
        var respQueue = new BlockingCollection<byte[]>();

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                respQueue.Add(body);
            }
        };

        Channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);

        return (respQueue, props);
    }

    private RabbitMqRpcResponse<K> ProcessRpcResponse(byte[] responseBody)
    {
        _logger.LogTrace("Begin");

        if (responseBody.Length == 0)
        {
            throw new RabbitMqRpcException(StatusCode.Internal, "Response is Empty!");
        }
        try
        {
            return MessagePackSerializer.Deserialize<RabbitMqRpcResponse<K>>(responseBody);
        }
        catch (MessagePackSerializationException e)
        {
            const string errorMessage = "Error while deserializing response";
            _logger.LogError(e, errorMessage);
            throw new RabbitMqRpcException(StatusCode.Internal, errorMessage);
        }
        catch (Exception ex)
        {
			const string errorMessage = "Unknown error while processing response";
            _logger.LogError(ex, errorMessage);
            throw new RabbitMqRpcException(StatusCode.Internal, errorMessage);
        }
    }
}
