namespace SML.Display.Core.Interfaces.Main;

/// <summary>
/// Interface of the main controller.
/// </summary>
public interface IMainController
{
    /// <summary>
    /// Start the controller.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop the controller.
    /// </summary>
    void Stop();
}
