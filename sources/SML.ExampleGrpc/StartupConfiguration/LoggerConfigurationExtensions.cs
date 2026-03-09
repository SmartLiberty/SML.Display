using System.IO.Compression;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File.Archive;
using SML.ExampleGrpc.Formatter;

namespace SML.ExampleGrpc.StartupConfiguration;

public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Creates the logger configuration PS: Minimal log level is set in appsettings.json for dynamic change
    /// </summary>
    /// <returns>the config</returns>
    /// <exception cref="InvalidOperationException">If the env var SML_INSTALL__DATA is not set</exception>
    public static LoggerConfiguration ConfigureLogger(this LoggerConfiguration loggerConfiguration, IConfiguration appConfiguration)
    {
        var serviceName = Assembly.GetExecutingAssembly().GetName().Name;
        var installData = Environment.GetEnvironmentVariable("SML_INSTALL__DATA") ?? throw new InvalidOperationException("SML_INSTALL__DATA environment variable is not; set.");
        var logPath = Path.Combine(installData, "Logs", serviceName, $"{serviceName}_.log");
        var archivePath = Path.Combine(installData, "Logs", serviceName, "Archive");
        var jsonLogPath = Path.Combine(installData, "JsonLogs", serviceName, $"{serviceName}_.json");
        
        // Normal Log format
        var textLayout = "{Timestamp:yyyy-MM-dd HH:mm:ss.ffff}|" + serviceName + "|{ThreadId}|{CustomLevel}|{SourceContext}|{MethodName}|{Message:lj}{NewLine}{Exception}";

        // JSON Log format
        var jsonTemplate = new LogJsonFormatter(serviceName);
        
        // Configure Logger
        loggerConfiguration
            // appsettings.json configuration only Log level
            .ReadFrom.Configuration(appConfiguration)

            // Enrichers
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithCustomMachineName()
            .Enrich.WithUniqueDate()
            .Enrich.WithMethodName(serviceName)
            .Enrich.WithCustomLevel()

            // Overrides
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Information)
            .MinimumLevel.Override("Grpc.Net.Client.Internal", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc.AspNetCore.Server", LogEventLevel.Warning)

            // Console output Only on DEBUG
            #if DEBUG
            .WriteTo.Console(outputTemplate: textLayout)
            #endif

            // Normal log file with archiving
            .WriteTo.File(
                path: logPath,
                outputTemplate: textLayout,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 1,
                fileSizeLimitBytes: 104_900_000,
                rollOnFileSizeLimit: true,
                shared: false,
                buffered: false,
                hooks: new ArchiveHooks(14, CompressionLevel.Fastest, archivePath)
            )

            // JSON log file
            .WriteTo.File(
                formatter: jsonTemplate,
                path: jsonLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 3,
                shared: true
            );
                
        
        return loggerConfiguration;
    }
    
}