namespace SML.Display.Core.Interfaces.Events;

/// <summary>
/// Interface for RabbitMQ base.
/// </summary>
public interface IRabbitMqBase
{
	/// <summary>
	/// Exchange name.
	/// </summary>
	string ExchangeName { get; }

	/// <summary>
	/// Start connection if necessary, create channel and call "Initialize" method.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Asynchronous task result.</returns>
	Task StartAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Call "End" method, close channel, then stop connection if last one to stop.
	/// </summary>
	void Stop();
}
