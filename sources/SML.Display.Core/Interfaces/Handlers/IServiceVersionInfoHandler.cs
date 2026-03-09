namespace SML.Display.Core.Interfaces.Handlers;

/// <summary>
/// Represents an interface for handling service version information.
/// </summary>
public interface IServiceVersionInfoHandler
{
    /// <summary>
    /// Starts the service version information.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the service version information.
    /// </summary>
    void Stop();
}
