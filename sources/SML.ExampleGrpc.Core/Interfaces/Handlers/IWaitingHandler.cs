namespace SML.ExampleGrpc.Core.Interfaces.Handlers;

/// <summary>
/// Interface of the waiting handler.
/// </summary>
public interface IWaitingHandler
{
    /// <summary>
    /// Wait until triggering.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with true if the wait ended with a trigger, false otherwise..</returns>
    Task<bool> WaitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Wait until triggering.
    /// </summary>
    /// <param name="timeout">Waiting timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with true if the wait ended with a trigger, false otherwise..</returns>
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Trigger end of wait.
    /// </summary>
    void Set();

    /// <summary>
    /// Reset the trigger end of wait.
    /// </summary>
    void Reset();
}
