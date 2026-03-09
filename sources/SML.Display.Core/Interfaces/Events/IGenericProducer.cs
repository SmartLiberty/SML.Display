namespace SML.Display.Core.Interfaces.Events;

/// <summary>
/// Interface for generic RabbitMQ producer.
/// </summary>
/// <typeparam name="T">Type of the DTO event to publish.</typeparam>
public interface IGenericProducer<T> : IRabbitMqBase where T : class
{
	/// <summary>
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	/// <returns>True whether the event has been published, false otherwise.</returns>
	bool Publish(T rmqEvent);

	/// <summary>
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	Task PublishAsync(T rmqEvent);

	/// <summary>
	/// Publish an event.
	/// </summary>
	/// <param name="rmqEvent">Event to publish.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task PublishAsync(T rmqEvent, CancellationToken cancellationToken);
}
