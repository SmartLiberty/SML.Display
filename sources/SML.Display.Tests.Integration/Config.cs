namespace SML.Display.Tests.Integration;

using Microsoft.Extensions.Configuration;

public static class Config
{
    public static IConfiguration InitConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build();
        return config;
    }
}
