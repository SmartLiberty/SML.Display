namespace SML.Display.StartupConfiguration;

using Core.Correlations.RpcInterceptors;
using Core.Data.Settings;
using Grpc.Net.Client.Configuration;
using Shared.Proto;

//TODO add gRPC clients configurations in this method. Remove it if no gRPC client is used
public static class GrpcClientConfigurationExtensions
{
    public static IServiceCollection ConfigureGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<CorrelationRpcServerInterceptor>();
        });

        var exampleGrpcClientSettings = configuration.GetSection("GrpcExampleClientSettings").Get<GrpcClientSettings>();

		var allowInvalidCertificate = configuration.GetValue<bool>("Kestrel:Certificates:Default:AllowInvalid");
       
        services.AddTransient<CorrelationRpcClientInterceptor>();


        //services.AddGrpcClient<Examples.ExamplesClient>(exampleGrpcClientSettings, allowInvalidCertificate);
        
        return services;
    }
    
    private static IServiceCollection AddGrpcClient<T>(this IServiceCollection serviceCollection, GrpcClientSettings grpcClientSettings, bool allowInvalidCertificate) where T : class
    {
        serviceCollection.AddGrpcClient<T>(options =>
        {
            options.Address = new Uri(grpcClientSettings.Url);
            options.ChannelOptionsActions.Add(channelOptions =>
			{
				if (allowInvalidCertificate)
				{
					channelOptions.HttpHandler = new HttpClientHandler()
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
					};
				}
				if (grpcClientSettings.RetryPolicy is { } retryPolicy)
				{
					channelOptions.MaxRetryAttempts = retryPolicy.MaxAttempts;
				}
				channelOptions.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = grpcClientSettings.RetryPolicy
                        }
                    }
                };
            });
        }).AddInterceptor<CorrelationRpcClientInterceptor>();

        return serviceCollection;
    }
}
