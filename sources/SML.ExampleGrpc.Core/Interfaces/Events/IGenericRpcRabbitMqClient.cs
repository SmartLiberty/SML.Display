namespace SML.ExampleGrpc.Core.Interfaces.Events;

/// <summary>
/// Interface for generic RPC RabbitMQ client.
/// </summary>
/// <typeparam name="T">Request type.</typeparam>
/// <typeparam name="K">Response type.</typeparam>
public interface IGenericRpcRabbitMqClient<T, K> : IRabbitMqBase
	where T : class
    where K : class?
{
    /// <summary>
    /// Publish a request.
    /// </summary>
    /// <param name="request">Request to publish by event.</param>
    /// <returns>Response.</returns>
    K? Publish(T request);
}