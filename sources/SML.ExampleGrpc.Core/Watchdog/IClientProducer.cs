namespace SML.ExampleGrpc.Core.Watchdog;

using Core.Interfaces.Events;

/// <summary>
/// Interface for generic RabbitMQ producer.
/// </summary>
/// <typeparam name="T">Type of the DTO event to publish.</typeparam>
[Obsolete("To be deleted after watchdog refactoring."
    + "Please do not use for anything other than obsolete watchdog.")]
public interface IClientProducer<T> : IRabbitMqBase where T : class
{
	/// <summary>
	/// Publish a DTO event.
	/// </summary>
	/// <param name="dtoEvent">DTO event to publish.</param>
	/// <returns>True wether the DTO event has been published, false otherwise.</returns>
	bool Publish(T dtoEvent);

	/// <summary>
	/// Publish a DTO event.
	/// </summary>
	/// <param name="dtoEvent">DTO event to publish.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task PublishAsync(T dtoEvent, CancellationToken cancellationToken);
}
