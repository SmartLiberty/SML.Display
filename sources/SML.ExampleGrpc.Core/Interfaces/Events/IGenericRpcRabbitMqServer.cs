namespace SML.ExampleGrpc.Core.Interfaces.Events;

/// <summary>
/// Interface for generic RPC RabbitMQ server.
/// </summary>
/// <typeparam name="T">Request type.</typeparam>
/// <typeparam name="K">Response type.</typeparam>
public interface IGenericRpcRabbitMqServer<T, K> : IRabbitMqBase
{
	/// <summary>
	/// Start connection if necessary, create channel and call "Initialize" method.
	/// </summary>
	/// <param name="function">Function called when a request is received. Reply follows.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Asynchronous task result.</returns>
	Task StartAsync(Func<T, K> function, CancellationToken cancellationToken);
}