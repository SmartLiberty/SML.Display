namespace SML.ExampleGrpc.StartupConfiguration;

using AutoMapper.Internal;
using Core.MappingProfiles;

public static class AutoMapperServiceCollectionExtensions
{
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.Internal().MethodMappingEnabled = false,
            typeof(TimeMappingProfile));

        return services;
    }
}