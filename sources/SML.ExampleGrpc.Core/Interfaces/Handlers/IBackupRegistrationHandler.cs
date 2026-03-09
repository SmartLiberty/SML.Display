namespace SML.ExampleGrpc.Core.Interfaces.Handlers;

// TODO Remove this interface if there is no backup registration

/// <summary>
/// Interface of backup registration handler.
/// </summary>
public interface IBackupRegistrationHandler
{
	/// <summary>
	/// Start the handler.
	/// </summary>
	Task StartAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Stop the handler.
	/// </summary>
	void Stop();
}