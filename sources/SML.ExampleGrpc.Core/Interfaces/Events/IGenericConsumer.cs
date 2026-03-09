namespace SML.ExampleGrpc.Core.Interfaces.Events;

/// <summary>
/// Interface for generic RabbitMQ consumer.
/// </summary>
/// <typeparam name="T">Type of the event to consume.</typeparam>
public interface IGenericConsumer<T> : IRabbitMqBase where T : class
{
    /// <summary>
    /// Event raised when an event is consumed.
    /// </summary>
    event Action<T> ConsumedEvent;
}
