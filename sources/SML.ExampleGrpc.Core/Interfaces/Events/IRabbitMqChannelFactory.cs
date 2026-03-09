namespace SML.ExampleGrpc.Core.Interfaces.Events;

using RabbitMQ.Client;

/// <summary>
/// Interface of RabbitMQ channel factory.
/// </summary>
public interface IRabbitMqChannelFactory : IDisposable
{
	/// <summary>
	/// Create a channel.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Asynchronous task result with created channel.</returns>
	Task<IModel> CreateChannelAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Increase the backoff for attempts.
	/// </summary>
	/// <param name="backoff">Previous backoff to increase.</param>
	/// <returns>Increased backoff.</returns>
	TimeSpan Increase(TimeSpan backoff);
}
