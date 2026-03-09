namespace SML.ExampleGrpc.StartupConfiguration;

using Core.Data.Settings;
using Core.Watchdog;
using Settings;

public static class SettingsConfigurationExtensions
{
    public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
    {
		//INFO ValidateDataAnnotations() will validate the settings on the injection of an IOption<T>. By adding ValidateOnStart(), the validation is executed during startup
		services.AddOptions<SiteSettings>().BindConfiguration("Site").ValidateDataAnnotations().ValidateOnStart();
		services.AddOptions<InstallSettings>().BindConfiguration("Install").ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<PostgresReconnectionSettings>().BindConfiguration("PostgresReconnectionSettings").ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<GeneralSettings>().BindConfiguration("GeneralSettings").ValidateDataAnnotations().ValidateOnStart();
        services.PostConfigure<GeneralSettings>(options =>
		{
			options.HealthCheckUrl = configuration.GetValue<string>("Kestrel:EndPoints:Https:Url") + HealthChecksConfigurationExtensions.EndpointHealthCheck;
		});

		services.AddOptions<LifeTimeSettings>().BindConfiguration("LifeTimeSettings").ValidateDataAnnotations().ValidateOnStart();

		services.Configure<RecurringJobSettings>(RecurringJobSettings.Watchdog, configuration.GetSection($"RecurringJobSettings:{RecurringJobSettings.Watchdog}"));
		services.Configure<RecurringJobSettings>(RecurringJobSettings.CleanArchives, configuration.GetSection($"RecurringJobSettings:{RecurringJobSettings.CleanArchives}"));
        services.Configure<RecurringJobSettings>(RecurringJobSettings.LoggOneLine, configuration.GetSection($"RecurringJobSettings:{RecurringJobSettings.LoggOneLine}"));

        var rabbitMqSettings = configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
		services.AddSingleton(rabbitMqSettings);
		services.AddOptions<WatchdogClientSettings>().BindConfiguration("WatchdogClientSettings").ValidateDataAnnotations().ValidateOnStart();
		services.AddScoped<ClientExchangeSettings>();

		return services;
    }
}