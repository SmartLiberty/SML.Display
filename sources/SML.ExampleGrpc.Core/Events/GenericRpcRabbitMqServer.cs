namespace SML.ExampleGrpc.Core.Events;

using Core.Data.Settings;
using Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SML.Shared.RabbitMq.Rpc;
using System;

/// <summary>
/// Generic RPC RabbitMQ server.
/// </summary>
/// <typeparam name="T">Request type.</typeparam>
/// <typeparam name="K">Response type.</typeparam>
public class GenericRpcRabbitMqServer<T, K> : RabbitMqBase, IGenericRpcRabbitMqServer<T, K>
    where T : class
    where K : class
{
    private readonly ILogger<GenericRpcRabbitMqServer<T, K>> _logger;

    private readonly bool _durable;
    	
	private EventingBasicConsumer? _consumer;
    private readonly string _queue;

	private Func<T, K>? _consumeAndReply;

    private readonly object _locker;

	public GenericRpcRabbitMqServer(ILogger<GenericRpcRabbitMqServer<T, K>> logger,
		IRabbitMqChannelFactory channelFactory,
		RabbitMqSettings settings,
        bool durable = false,
        string? queueSuffix = null)
        : base(channelFactory, settings, $"{typeof(T).FullName}-rpc")
    {
        _logger = logger;
        _logger.LogTrace("");

        _durable = durable;
        _queue = $"{ExchangeName}-{GetType().Namespace}.RpcServer";
        if (queueSuffix != null)
        {
            _queue = $"{_queue}-{queueSuffix}";
        }

        _locker = new();
    }

	/// <summary>
	/// Start connection if necessary, create channel and call "Initialize" method.
	/// </summary>
	/// <param name="function">Function called when a request is received. Reply follows.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Asynchronous task result.</returns>
	public async Task StartAsync(Func<T, K> function, CancellationToken cancellationToken)
	{
		_consumeAndReply = function;
		await StartAsync(cancellationToken);
	}

	/// <summary>
	/// Initialize RabbitMQ communication.
	/// </summary>
	protected override void Initialize(IModel channel)
	{
        if (_consumeAndReply == null)
        {
            Stop();
			throw new NotSupportedException("Use Start function with function to consume and reply");
		}
		channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, true);
        var queue = channel.QueueDeclare(
            queue: _queue,
            durable: _durable,
            exclusive: false,
            autoDelete: !_durable,
            arguments: null).QueueName;

		channel.QueueBind(queue, ExchangeName, routingKey: "");

		channel.BasicQos(0, 1, false);

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
        try
        {
            var requestProps = arguments.BasicProperties;
            var replyProps = Channel!.CreateBasicProperties();
            replyProps.CorrelationId = requestProps.CorrelationId;

            try
            {
                var request = MessagePackSerializer.Deserialize<T>(arguments.Body);
                ConsumeAndReply(arguments, requestProps, request);
            }
            catch (MessagePackSerializationException exDeserialize)
            {
                const string deserializeError = "Fail to deserialize";
                _logger.LogError(exDeserialize, deserializeError);

                ReplyWithError(requestProps.CorrelationId,
                    requestProps.ReplyTo,
                    arguments.DeliveryTag, (int)StatusCode.BadRequest,
                    $"{deserializeError}. {exDeserialize.Message}");
            }
            catch (RabbitMqRpcException rcpEx)
            {
                _logger.LogError(rcpEx, rcpEx.Message);

                ReplyWithError(requestProps.CorrelationId,
                        requestProps.ReplyTo,
                        arguments.DeliveryTag, rcpEx.ErrorCode,
                        $"{rcpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                ReplyWithError(requestProps.CorrelationId,
                    requestProps.ReplyTo,
                    arguments.DeliveryTag, (int)StatusCode.Internal,
                    $"{ex.Message}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "There was a problem while trying to process Received Message");
        }
    }

    private void ConsumeAndReply(BasicDeliverEventArgs arguments, IBasicProperties requestProps, T request)
    {
        if (_consumeAndReply is not { } consumeAndReply)
        {
            ReplyWithError(requestProps.CorrelationId,
                    requestProps.ReplyTo,
                    arguments.DeliveryTag,
                    (int)StatusCode.Internal,
                    $"Missing function to consume and reply");
            return;
        }

        try
        {
            var replyData = consumeAndReply.Invoke(request)
                ?? throw new NotSupportedException("Missing function to consume and reply");

            ReplySuccess(requestProps.CorrelationId,
                requestProps.ReplyTo,
                arguments.DeliveryTag,
                replyData);
        }
        catch (RabbitMqRpcException ex)
        {
            _logger.LogError(ex, ex.Message);

            ReplyWithError(requestProps.CorrelationId,
                requestProps.ReplyTo,
                arguments.DeliveryTag,
                ex.ErrorCode,
                ex.Message);
        }
        catch (Exception exHandle)
        {
            const string handlerError = "Fail to handle";
            _logger.LogError(exHandle, handlerError);

            ReplyWithError(requestProps.CorrelationId,
                requestProps.ReplyTo,
                arguments.DeliveryTag,
                (int)StatusCode.Internal,
                $"{handlerError}. {exHandle.Message}");
        }
    }

    private void ReplyWithError(string correlationId, string replyTo, ulong deliveryTag, int errorCode, string errorMessage)
    {
        _logger.LogTrace("Reply with error");

        var errorResponse = new RabbitMqRpcResponse<K>
        {
            StatusCode = errorCode,
            ErrorMessage = errorMessage
        };

        var responseBody = MessagePackSerializer.Serialize(errorResponse);
        PublishReply(correlationId, replyTo, deliveryTag, responseBody);
    }

    private void ReplySuccess(string correlationId, string replyTo, ulong deliveryTag, K replyData)
    {
        _logger.LogTrace("Reply with success");

        var successResponse = new RabbitMqRpcResponse<K>
        {
            StatusCode = (int)StatusCode.OK,
            Message = replyData
        };

        var responseBody = MessagePackSerializer.Serialize(successResponse);
        PublishReply(correlationId, replyTo, deliveryTag, responseBody);
    }

    private void PublishReply(string correlationId, string replyTo, ulong deliveryTag, byte[] responseBody)
    {
        var replyProps = Channel!.CreateBasicProperties();
        replyProps.CorrelationId = correlationId;

        // Lock mandatory to prevent concurrency errors
        lock (_locker)
        {
            Channel.BasicPublish(
                exchange: "",
                routingKey: replyTo,
                basicProperties: replyProps,
                body: responseBody);
        }

        Channel.BasicAck(
            deliveryTag: deliveryTag,
            multiple: false);
    }
}
