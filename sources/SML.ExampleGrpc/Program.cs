using Serilog;
using SML.VersionInfos.Events;

namespace SML.ExampleGrpc;

using Core.Database;
using Core.Helpers;
using Core.Interfaces.Events;
using Kestrel.HttpsCertificateSelection;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.EntityFrameworkCore;
using StartupConfiguration;
using Steeltoe.Extensions.Configuration.Placeholder;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

/// <summary>
/// Program of the service.
/// </summary>
public class Program
{
    /// <summary>
    /// Main method of the service.
    /// </summary>
    /// <param name="args">Input arguments.</param>
    public static void Main(string[] args)
    {
        // Rapid logger Configuration for startup
        var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false,
            reloadOnChange: true)
         .AddEnvironmentVariables("SML_")
         .Build();
        
        Log.Logger = new LoggerConfiguration()
            .ConfigureLogger(configuration)
            .CreateBootstrapLogger();
        
        var logger = Log.Logger.ForContext<Program>();
            
        try
        {
            logger.Information("Program is building Host");
            var host = CreateHostBuilder(args).Build();

            if (RunRemoveServiceOnly(args))
            {
                logger.Information("Send remove message");
                RemoveServiceOnly(host);
                return;
            }

            if (RunDatabaseUpdateOnly(args)) // TODO Must be handled even if no database
            {
                logger.Information("Program is executing database migration");
                MigrateDatabase(host);
                return;
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development
                                && !args.Contains("--No-Database-Update"))
            {
                logger.Information("Program is executing database migration");
                MigrateDatabase(host);
            }

            logger.Information("Start running the application");
            host.Run();

            logger.Information("Application shutdown ended");
        }
        catch (Exception ex)
        {
            EnvironmentHelper.FailFast(logger, $"{nameof(Program)} crashed", ex);
        }
    }

    private static bool RunDatabaseUpdateOnly(string[] args)
    {
        return args.Contains("--Database-Update");
    }

    private static bool RunRemoveServiceOnly(string[] args)
    {
        return args.Contains("--Remove-Service");
    }

    private static void RemoveServiceOnly(IHost host)
    {
        var logger = Log.Logger.ForContext<Program>();
        using var scope = host.Services.CreateScope();

        logger.Information("Start version producer");
        var producer = host.Services.GetRequiredService<IGenericProducer<VersionEvent>>();
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		producer.StartAsync(cancellationToken).Wait(cancellationToken);

		var versionEvent = new VersionEvent { ServiceName = Assembly.GetExecutingAssembly().GetName().Name, Version = "" };
        logger.Information("Publish version {@ServiceStartedEvent}", versionEvent);
        producer.Publish(versionEvent);
        producer.Stop();
    }

    /// <summary>
    /// Configures the HTTPS settings for a specific endpoint in the Kestrel configuration.
    /// </summary>
    /// <param name="kernelConf">The Kestrel configuration loader.</param>
    /// <param name="endpointName">The name of the endpoint to configure.</param>
    /// <param name="configuration">The application configuration from which to retrieve certificate settings.</param>
    private static void ConfigureEndpoint(KestrelConfigurationLoader kernelConf, string endpointName, IConfiguration configuration)
    {
        kernelConf.Endpoint(endpointName, endpointConfiguration =>
        {
            endpointConfiguration.HttpsOptions.ConfigureLocalStoreServerCertificateSelection(endpointConfiguration.ListenOptions, configOption =>
            {
                string pollingIntervalString = configuration.GetValue<string>("Kestrel:Certificates:Default:PollingInterval")!;

                configOption.FindType = X509FindType.FindBySubjectName;
                configOption.FindValue = configuration.GetValue<string>("Kestrel:Certificates:Default:Subject");
                configOption.Location = configuration.GetValue<string>("Kestrel:Certificates:Default:Location") == "LocalMachine" ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
                configOption.StoreName = configuration.GetValue<string>("Kestrel:Certificates:Default:Store");
                configOption.PollingInterval = TimeSpan.Parse(pollingIntervalString);
                configOption.ValidCertificatesOnly = !configuration.GetValue<bool>("Kestrel:Certificates:Default:AllowInvalid");
            });
        });
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables("SML_");
            })
            .AddPlaceholderResolver()
            .UseSerilog((context, services, configuration) => 
            {
                // Changing logger configuration to support dynamic settings
                configuration.ConfigureLogger(context.
                Configuration);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                //if we are windows user, certificate store is available
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    webBuilder.UseKestrel((context, serverOptions) =>
                    {
                        var kernelConf = serverOptions.Configure(context.Configuration.GetSection("Kestrel"));
                        //If certificate store is set -> replace certificate selector
                        if (!string.IsNullOrEmpty(context.Configuration.GetValue<string>("Kestrel:Certificates:Default:Store")))
                        {
                            ConfigureEndpoint(kernelConf, "Https", context.Configuration);
                        }
                    });
                }

                webBuilder.UseStartup<Startup>();
            })
            .ConfigureServices(serviceCollection =>
            {
                // Register hosted services after web host configuration is done
                serviceCollection.ConfigureHostedServices();
            });
    }

    private static void MigrateDatabase(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        context.Database.Migrate();
    }
}