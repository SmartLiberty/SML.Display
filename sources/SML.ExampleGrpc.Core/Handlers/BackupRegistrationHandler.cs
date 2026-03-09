namespace SML.ExampleGrpc.Core.Handlers;

using Core.Data.Settings;
using Core.Interfaces.Events;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SML.Backup.Shared.Events;
using System.Threading;

// TODO Remove this handler if there is no backup registration

/// <summary>
/// Handler for backup registration.
/// </summary>
public class BackupRegistrationHandler : IBackupRegistrationHandler
{
    private readonly ILogger<BackupRegistrationHandler> _logger;

	private readonly IGenericConsumer<RmqBackupRegistrationsRequestEvent> _registrationsRequestEventsConsumer;
	private readonly IGenericProducer<RmqBackupRegistrationEvent> _registrationEventsProducer;

	private readonly RmqBackupRegistrationEvent _registrationEvent;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="logger">Logger.</param>
	/// <param name="registrationsRequestEventsConsumer">Backup registrations request events consumer.</param>
	/// <param name="registrationEventsProducer">Backup registration events producer.</param>
	/// <param name="generalSettings">General settings.</param>
	public BackupRegistrationHandler(
        ILogger<BackupRegistrationHandler> logger,
		IGenericConsumer<RmqBackupRegistrationsRequestEvent> registrationsRequestEventsConsumer,
		IGenericProducer<RmqBackupRegistrationEvent> registrationEventsProducer,
		IOptions<GeneralSettings> generalSettings)
    {
        _logger = logger;
        _logger.LogTrace("");

		_registrationsRequestEventsConsumer = registrationsRequestEventsConsumer;
		_registrationEventsProducer = registrationEventsProducer;

		var settings = generalSettings.Value;
		_registrationEvent = new RmqBackupRegistrationEvent
		{
			ServiceName = settings.ServiceName
		};
		//_registrationEvent.Registrations.Add(new RmqDatabaseBackupRegistration
		//{
		//	Name = settings.ServiceName
		//});
		//_registrationEvent.Registrations.Add(new RmqFileBackupRegistration
		//{
		//	Name = settings.ServiceName,
		//	Location = "Path/",
		//	Extension = ".xyz"
		//});
		//_registrationEvent.Registrations.Add(new RmqFolderBackupRegistration
		//{
		//	Name = settings.ServiceName,
		//	Location = "Path/"
		//});

		/* TODO WARNING !
		At least one DtoBackupItemRegistration
			Never set a BackupType to Unknown
			For Database
				Set a Name
			For File
				Set a Name
				Set an Extension
				Set a Location
			For Folder
				Set a Name
				Set a Location
		Delete this comment once checked */
	}

	/// <inheritdoc/>
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogTrace("");

		_registrationsRequestEventsConsumer.ConsumedEvent += OnRegistrationRequest;

		await _registrationEventsProducer.StartAsync(cancellationToken);

		await _registrationsRequestEventsConsumer.StartAsync(cancellationToken);

		await _registrationEventsProducer.PublishAsync(_registrationEvent, cancellationToken);
	}

    /// <inheritdoc/>
    public void Stop()
	{
		_logger.LogTrace("");

		_registrationsRequestEventsConsumer.Stop();

		_registrationEventsProducer.Stop();

		_registrationsRequestEventsConsumer.ConsumedEvent -= OnRegistrationRequest;
	}

    private void OnRegistrationRequest(RmqBackupRegistrationsRequestEvent _)
	{
		_logger.LogTrace("");

		_registrationEventsProducer.PublishAsync(_registrationEvent).Wait();
	}
}