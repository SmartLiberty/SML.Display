namespace SML.ExampleGrpc.StartupConfiguration;

using Calzolari.Grpc.AspNetCore.Validation;
using Core.Main;
using Core.Validators;
using FluentValidation;
using Middleware.Interceptors;
using Scrutor;

public static class GrpcServiceConfigurationExtensions
{
    public static void EnsureGrpcPortIsConfigured(this IServiceCollection services, IConfiguration configuration)
    {
        //This check can be removed after getting a port for gRPC
        var url = configuration.GetValue<string>("Kestrel:EndPoints:Https:Url");
        if (url.Contains("GET AN AVAILABLE PORT"))
        {
            throw new InvalidDataException($"URL of the gRPC settings is invalid! The port is missing. URL: {url}");
        }
    }
    
    public static IServiceCollection ConfigureGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionInterceptor>();
            options.EnableMessageValidation();
        });
        
        services
            .AddGrpcValidation()
            .AddGrpcValidators();

        return services;
    }

    public static void MapGrpcServices(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<GrpcExamplesServices>();
    }

    private static IServiceCollection AddGrpcValidators(this IServiceCollection serviceCollection)
    {
        serviceCollection.Scan(scan =>
            scan.FromAssemblyOf<GrpcUpdateRequestValidator>()
                .AddClasses(classes => classes.AssignableTo(typeof(AbstractValidator<>)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces());

        return serviceCollection;
    }
}