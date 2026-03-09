namespace SML.Display.StartupConfiguration;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using Core.Watchdog;
using Core.Watchdog.Dtos;
using RabbitMQ.Client;
using SML.Example.Shared.Dtos;
using SML.VersionInfos.Events;

/// <summary>
/// Provides extension methods for configuring RabbitMQ settings in the service collection.
/// </summary>
public static class RabbitMqConfigurationExtensions
{
	/// <summary>
	/// Configures RabbitMQ settings in the service collection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="configuration">The configuration source containing RabbitMQ settings.</param>
	/// <returns>The modified service collection.</returns>
	public static IServiceCollection ConfigureRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqSettings = configuration.GetRequiredSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>()!;
        
        services.AddSingleton<IConnectionFactory>(new ConnectionFactory
        {
            HostName = rabbitMqSettings.HostName,
            Port = rabbitMqSettings.Port,
            UserName = rabbitMqSettings.UserName,
            Password = rabbitMqSettings.Password
        });

		services.AddSingleton<IRabbitMqChannelFactory, RabbitMqChannelFactory>();

		services.AddSingleton<IGenericConsumer<VersionRequestEvent>, GenericConsumer<VersionRequestEvent>>();
		services.AddSingleton<IGenericConsumer<DtoExampleEvent>>(s => ActivatorUtilities.CreateInstance<GenericConsumer<DtoExampleEvent>>
			(s, DtoExampleEventRoutingKeyHelper.CreateConsumerKey(displayName: null)));
        // TODO Example of durable queue consumer
        //services.AddSingleton<IGenericConsumer<DtoExampleEvent>>(s =>
        //    ActivatorUtilities.CreateInstance<GenericConsumer<DtoExampleEvent>>(s, true));

        services.AddSingleton<IGenericProducer<VersionEvent>, GenericProducer<VersionEvent>>();
		services.AddSingleton<IGenericProducer<DtoExampleEvent>>(s => ActivatorUtilities.CreateInstance<GenericProducer<DtoExampleEvent>>
			(s, (DtoExampleEvent dtoEvent) => DtoExampleEventRoutingKeyHelper.CreateProducerKey(dtoEvent)));

		services.AddScoped<IClientProducer<DtoServiceRegistration>, ClientProducer<DtoServiceRegistration>>();
		services.AddScoped<IClientProducer<DtoEventTriggered>, ClientProducer<DtoEventTriggered>>();

		return services;
    }
}
