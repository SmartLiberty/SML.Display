namespace SML.Display.StartupConfiguration;

using HostedServices;

/// <summary>
/// Provides extension methods for configuring hosted services.
/// </summary>
public static class HostedServiceConfigurationExtensions
{
	/// <summary>
	/// Configures hosted services in the service collection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The modified service collection.</returns>
	public static IServiceCollection ConfigureHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<HealthCheckHostedService>();
        services.AddHostedService<MainHostedService>();
        //TODO add here any additional hosted service. Note : hosted services are started in the same order they are registered 
		services.AddHostedService<BackupRegistrationHostedService>();
		services.AddHostedService<CleanArchivesHostedService>();
		services.AddHostedService<ServiceVersionInfoHostedService>();
		services.AddHostedService<WatchdogHostedService>();
        services.AddHostedService<DatabaseAccessCheckerHostedService>();
        services.AddHostedService<LoggOneLineHostedService>();
        return services;        
    }
}
