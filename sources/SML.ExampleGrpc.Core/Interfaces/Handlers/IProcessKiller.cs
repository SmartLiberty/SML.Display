namespace SML.ExampleGrpc.Core.Interfaces.Handlers;

/// <summary>
/// Interface of the service process killer.
/// </summary>
public interface IProcessKiller
{
	/// <summary>
	/// Kill the current service process.
	/// </summary>
	/// <param name="reason">Reason of the service process killing.</param>
	void Kill(string reason);
}
