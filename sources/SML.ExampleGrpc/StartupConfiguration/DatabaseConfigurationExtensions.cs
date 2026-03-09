namespace SML.ExampleGrpc.StartupConfiguration;

using Core.Database;
using Microsoft.EntityFrameworkCore;

public static class DatabaseServiceCollectionExtensions
{
    internal const string DbContextName = "DbContext";
    
    public static IServiceCollection ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DatabaseContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionBuilder(configuration, optionsBuilder);
        }, contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);

        services.AddDbContextFactory<DatabaseContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionBuilder(configuration, optionsBuilder);
        });

        return services;
    }
    
    private static void ConfigureDbContextOptionBuilder(IConfiguration configuration, DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(configuration.GetConnectionString(DbContextName));
        optionsBuilder.UseSnakeCaseNamingConvention();
    }
}