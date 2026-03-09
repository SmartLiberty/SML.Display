namespace SML.Display.StartupConfiguration;

using Core.Handlers;
using Core.HealthChecks;
using Core.Interfaces.Handlers;
using Core.Interfaces.HealthChecks;
using Core.Interfaces.Main;
using Core.Main;
using Core.Watchdog;
using Scrutor;
using System.Reflection;

/// <summary>
/// Injection of all dependencies.
/// </summary>
public static class IoCConfigurationExtensions
{
    /// <summary>
    /// Inject all dependencies.
    ///
    /// The injection use "Scrutor" (https://github.com/khellang/Scrutor) to automatically inject dependencies found in SML.* assemblies as transient.
    /// Other dependencies or dependencies that require an other scope have to be added manually and before the Scrutor scan
    /// </summary>
    /// <param name="services">Service collection.</param>
    public static IServiceCollection ConfigureIoC(this IServiceCollection services)
    {
        services.AddSingleton<IWatchdogClientService, WatchdogClientService>();

        services.AddSingleton<IMainController, MainController>();
		services.AddSingleton<IDataHandler, DataHandler>();
		services.AddSingleton(TimeProvider.System);

		Assembly assembly = Assembly.GetExecutingAssembly();
		services.AddSingleton((Func<IServiceProvider, IServiceVersionInfoHandler>)(s => ActivatorUtilities.CreateInstance<ServiceVersionInfoHandler>(s, assembly)));
        services.AddSingleton<IDatabaseAccessChecker, DatabaseAccessChecker>();

        services.AddSingleton<IStartupHealthChecks, StartupHealthChecks>();

        // SML assemblies
        IEnumerable<Assembly> smlAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName != null && a.FullName.StartsWith("SML."));
        
        //add class names here that should be excluded from automatic injection
        var excludedTypes = new List<string>(); 

        services.Scan(scan =>
        {
            scan.FromAssemblies(smlAssemblies)
                .AddClasses(c => c.Where(t => !excludedTypes.Contains(t.Name)))
                .AsMatchingInterface()
                .UsingRegistrationStrategy(RegistrationStrategy.Skip);
        });
        
        return services;
    }
}
