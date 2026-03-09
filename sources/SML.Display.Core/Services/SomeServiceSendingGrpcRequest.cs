namespace SML.Display.Core.Services;

using Grpc.Net.ClientFactory;
using Interfaces.Services;
using Shared.Proto;

//TODO remove these classes. This is only examples showing how to use gRPC client, it is not actually used (it would not make sense to call the service from itself).
//More information in KB : https://smartliberty.knowledgebase.co/secure/11/grpc-client-validation-and-error-handling-580.html

public class SomeServiceSendingGrpcRequest : ISomeServiceSendingGrpcRequest
{
    private readonly Examples.ExamplesClient _examplesClient;
 
    public SomeServiceSendingGrpcRequest(Examples.ExamplesClient examplesClient)
    {
        _examplesClient = examplesClient;
    }

    public async Task DoSomething()
    {
        var result = await _examplesClient.ReadAsync(new GrpcIdRequest
        {
            Id = 123
        });

        // Do something with the request result
    }
}

public class SomeServiceSendingGrpcRequestUsingFactory : ISomeServiceSendingGrpcRequest
{
    private readonly GrpcClientFactory _grpcClientFactory;
 
    public SomeServiceSendingGrpcRequestUsingFactory(GrpcClientFactory grpcClientFactoryFactory)
    {
        _grpcClientFactory = grpcClientFactoryFactory;
    }

    public async Task DoSomething()
    {
        //by default, the client is registered using its type name as instance name. If a specific name is required it has to be set by passing the name to the AddGrpcClient method in IoC configuration
        var client = _grpcClientFactory.CreateClient<Examples.ExamplesClient>(nameof(Examples.ExamplesClient));
        var result = await client.ReadAsync(new GrpcIdRequest
        {
            Id = 123
        });

        // Do something with the request result
    }
}
