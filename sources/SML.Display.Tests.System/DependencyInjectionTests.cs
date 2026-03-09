namespace SML.Display.Tests.System;

using AutoMapper;
using Core.Interfaces.Main;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StartupConfiguration;

[TestClass]
public class DependencyInjectionTests
{
    private static ServiceProvider _serviceProvider = null!;

    [ClassInitialize]
    public static void OneTimeSetup(TestContext _)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DbContext", "Host=localhost; Port=5432; Database='Test'; Username='postgres'; Password=''"},
                { "Install:Data", "D:/SmartLiberty"},
                { "GeneralSettings:ServiceName", "SML.Display"},
                { "GeneralSettings:ServiceDescription", "SML.Display"},
                { "GeneralSettings:HealthCheckUrl", "URL"},
                { "GrpcSettings:Url", "http://localhost:50000"},
                { "LifeTimeSettings:ArchivedExamplesDays", "30"},
                { "LogSettings:Config", ""},
                { "RabbitMqSettings:HostName", "localhost"},
                { "RabbitMqSettings:Port", "5742"},
                { "RabbitMqSettings:UserName", "guest"},
                { "RabbitMqSettings:Password", "guest"},
                { "WatchdogClientSettings:ServiceDisplayName", "SML.Messages"},
                { "WatchdogClientSettings:ServiceExe", "SML.Messages.exe"},
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .ConfigureSettings(configuration)
            .ConfigureDbContext(configuration)
            .ConfigureAutoMapper()
            .ConfigureGrpcServices()
            .ConfigureGrpcClients(configuration)
            .ConfigureRabbitMq(configuration)
            .ConfigureHealthChecks()
            .ConfigureHostedServices()
            .ConfigureIoC();

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void TestIMapper()
    {
        Assert.IsNotNull(_serviceProvider.GetService<IMapper>());
    }

    [TestMethod]
    public void TestTimeProvider()
    {
        Assert.IsNotNull(_serviceProvider.GetService<TimeProvider>());
    }
}
