namespace SML.Display.Core.Watchdog;

using Dtos;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Client Service to help other services with service and event registration and event triggers
/// </summary>
[Obsolete("To be deleted after watchdog refactoring.")]
[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IWatchdogClientService
{
    /// <summary>
    /// True whether there was already a request to register the current service, false otherwise.
    /// </summary>
    bool IsRegistered { get; }

    /// <summary>
    /// Register a service in Watchdog with the inputted list of events
    /// </summary>
    /// <param name="serviceEvents">List of Event details to be registered</param>
    Task RegisterServiceAsync(CancellationToken cancellationToken, params DtoEventRegistration[] serviceEvents);

    /// <summary>
    /// Calls Service Event Client to notify Watchdog of an event that was triggered
    /// </summary>
    /// <param name="eventId">Registered Event Id</param>
    Task EventTriggeredAsync(CancellationToken cancellationToken, long eventId);

    /// <summary>
    /// Confirm example operation.
    /// </summary>
    /// <param name="timeout">Timeout in seconds.</param>
    void ConfirmExampleOperation(int timeout);
}
