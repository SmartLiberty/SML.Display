namespace SML.ExampleGrpc;

using StartupConfiguration;

/// <summary>
/// Startup.
/// </summary>
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    /// <summary>
    /// Inject gRPC service.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <remarks>This method gets called by the runtime.
    /// For more information on how to configure your service, visit https://go.microsoft.com/fwlink/?LinkID=398940 </remarks>
    public void ConfigureServices(IServiceCollection services)
    {
        services.EnsureGrpcPortIsConfigured(Configuration);
        
        services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);
        services
            .ConfigureSettings(Configuration)
            .ConfigureDbContext(Configuration)
            .ConfigureAutoMapper()
            .ConfigureHealthChecks()
            .ConfigureGrpcServices()
            .ConfigureGrpcClients(Configuration)
            .ConfigureRabbitMq(Configuration)
            .ConfigureIoC();
        //IMPORTANT any configuration that register dependency has to be called BEFORE ConfigureIoC
    }

    /// <summary>
    /// Configure gRPC service and database.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <param name="environment">Web host environment.</param>
    /// <remarks>This method gets called by the runtime.</remarks>
    public void Configure(IApplicationBuilder builder, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            builder.UseDeveloperExceptionPage();
        }

        builder.UseRouting();

        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcServices();

            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            
            });
            
            endpoints.MapHealthChecks();
        });
    }
}