namespace SML.Display.Core.Watchdog;

using Core.Data.Settings;
using Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <inheritdoc />
// ReSharper disable once UnusedMember.Global
[Obsolete("To be deleted after watchdog refactoring.")]
public class WatchdogClientService : IWatchdogClientService
{
    private const string RegistrationExchangeName = "SML.Watchdog.Registration";

    public bool IsRegistered { get; private set; }

    private readonly ILogger<WatchdogClientService> _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly GeneralSettings _generalSettings;
    private readonly WatchdogClientSettings _watchdogClientSettings;
    private readonly TimeProvider _timeProvider;

    private readonly Dictionary<long, DateTime> _eventTriggeredDateTimes;

    private List<DtoEventRegistration> _serviceEvents;

    private DateTime _exampleOperationExpiration; // TODO Replace or remove

    /// <summary>
    /// Constructor.
    /// Define a Watchdog Service to allow Registration and Event Triggering in Watchdog
    /// </summary>
    /// <param name="logger">ILogger for WatchdogClientService - Used with New services</param>
    /// <param name="watchdogClientSettings">Watchdog client settings to be used</param>
    public WatchdogClientService(
        ILogger<WatchdogClientService> logger,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        IOptions<GeneralSettings> generalSettings,
        IOptions<WatchdogClientSettings> watchdogClientSettings)
    {
        _logger = logger;

        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;

        _generalSettings = generalSettings.Value;
        _watchdogClientSettings = watchdogClientSettings.Value;

        _eventTriggeredDateTimes = new Dictionary<long, DateTime>();

        _serviceEvents = new List<DtoEventRegistration>();
    }

    /// <inheritdoc/>
    public async Task RegisterServiceAsync(CancellationToken cancellationToken, params DtoEventRegistration[] serviceEvents)
    {
        try
        {
            _serviceEvents = serviceEvents.ToList();

            var registrationRequest = new DtoServiceRegistration
            {
                DisplayName = _generalSettings.ServiceName,
                Exe = _watchdogClientSettings.ServiceExe,
                Events = _serviceEvents
            };

            using var serviceScope = _serviceProvider.CreateScope();
            var exchangeSettings = serviceScope.ServiceProvider.GetRequiredService<ClientExchangeSettings>();
            exchangeSettings.Name = RegistrationExchangeName;
            var registrationClient = serviceScope.ServiceProvider.GetRequiredService<IClientProducer<DtoServiceRegistration>>();

            await registrationClient.StartAsync(cancellationToken);
            await registrationClient.PublishAsync(registrationRequest, cancellationToken);
            registrationClient.Stop();

            IsRegistered = true;
            _logger.LogInformation("Registered");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "");
        }
    }

    /// <inheritdoc />
    public async Task EventTriggeredAsync(CancellationToken cancellationToken, long eventId)
    {
        if (!IsRegistered)
        {
            return;
        }

        if (HasTimeoutExpired())
        {
            _logger.LogWarning("Do not publish triggered event because at least one checking timeout has expired.");
            return;
        }

        _logger.LogTrace("event_id:{EventTriggeredId}", eventId);

        var serviceEvent = _serviceEvents?.SingleOrDefault(e => e.Id == eventId);
        if (serviceEvent == null)
        {
            var exception = new InvalidDataException($"There is no Service Event registered with id {eventId}!");
            _logger.LogError(exception, "There was a problem trying to process Event Triggered" +
                                        "|event_id:{EventTriggeredId};{ExceptionMessage}",
                eventId, exception.Message);
            throw exception;
        }

        using var serviceScope = _serviceProvider.CreateScope();
        var exchangeSettings = serviceScope.ServiceProvider.GetRequiredService<ClientExchangeSettings>();
        exchangeSettings.Name = $"{_generalSettings.ServiceName}.{serviceEvent.DisplayName}";
        var serviceEventClient = serviceScope.ServiceProvider.GetRequiredService<IClientProducer<DtoEventTriggered>>();

        var eventTriggered = new DtoEventTriggered
        {
            Id = eventId,
            Timestamp = _timeProvider.GetUtcNow().DateTime,
        };

        _logger.LogTrace("event_triggered:{EventTriggered}", eventTriggered);

        //get the last datetime when this event was triggered (default if never)
        var lastTriggeredDateTime = _eventTriggeredDateTimes.TryGetValue(eventTriggered.Id, out var triggeredDateTime) ? triggeredDateTime : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        if ((eventTriggered.Timestamp - lastTriggeredDateTime).TotalSeconds < serviceEvent.Period / 2.0)
        {
            _logger.LogTrace("Not sent event_triggered:{EventTriggered}", eventTriggered);
            return;
        }

        //save the last triggered value
        _eventTriggeredDateTimes[eventTriggered.Id] = eventTriggered.Timestamp;

        await serviceEventClient.StartAsync(cancellationToken);
        if (serviceEventClient.Publish(eventTriggered))
        {
            _logger.LogTrace("Sent event_triggered:{EventTriggered};event_id:{EventTriggeredId};event_name:{EventTriggeredName}",
                eventTriggered, eventId, serviceEvent.DisplayName);
        }
        else
        {
            _logger.LogWarning("Impossible to publish event_triggered:{EventTriggered}", eventTriggered);
        }
        serviceEventClient.Stop();
    }

    /// <summary>
    /// Confirm example operation.
    /// </summary>
    /// <param name="timeout">Timeout in seconds.</param>
    public void ConfirmExampleOperation(int timeout) // TODO Replace or remove
        => _exampleOperationExpiration = _timeProvider.GetUtcNow().DateTime.AddSeconds(timeout);

    private bool HasTimeoutExpired()
        => false; // TODO _timeProvider.UtcNow > _exampleOperationExpiration;
}
