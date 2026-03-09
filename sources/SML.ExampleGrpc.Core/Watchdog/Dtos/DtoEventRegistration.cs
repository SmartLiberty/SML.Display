namespace SML.ExampleGrpc.Core.Watchdog.Dtos;

using MessagePack;

/// <summary>
/// Stores all information necessary for registering a Service event
/// </summary>
[Obsolete("To be deleted after watchdog refactoring.")]
[MessagePackObject(true)]
public class DtoEventRegistration
{
    /// <summary>
    /// Identification number of the Event
    /// </summary>
    public long Id { get; set; } = -1;

    /// <summary>
    /// Name of the Event to be displayed
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Interval of seconds in which the Event Message must be received
    /// Default = 60
    /// </summary>
    public int Period { get; set; } = 60;

    /// <summary>
    /// Interval of seconds which is accepted as a delay for an Event Message to be received.
    /// Default = 120
    /// </summary>
    public int GracePeriod { get; set; } = 120;
}