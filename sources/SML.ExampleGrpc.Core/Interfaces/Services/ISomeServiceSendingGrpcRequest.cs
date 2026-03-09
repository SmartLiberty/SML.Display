namespace SML.ExampleGrpc.Core.Interfaces.Services;

//TODO remove this interface. This is only an example showing how to use gRPC client, it is not actually used
public interface ISomeServiceSendingGrpcRequest
{
    Task DoSomething();
}