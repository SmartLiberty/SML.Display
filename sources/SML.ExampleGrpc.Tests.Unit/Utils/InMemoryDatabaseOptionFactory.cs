namespace SML.ExampleGrpc.Tests.Unit.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class InMemoryDatabaseOptionFactory
{
    public static DbContextOptionsBuilder<TContext> Create<TContext>(string inMemoryDatabaseName, TimeProvider timeProvider) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        builder.UseInMemoryDatabase(inMemoryDatabaseName);

        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        services.AddSingleton(timeProvider);
        builder.UseInternalServiceProvider(services.BuildServiceProvider());
        return builder;
    }
}