namespace SML.Display.Core.Correlations.RpcInterceptors;

using Core.Correlations.ThreadStorages;
using Grpc.Core;
using Grpc.Core.Interceptors;

public class CorrelationRpcClientInterceptor : Interceptor
{

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata
        {
            { CorrelationIdThreadStorageHolder.CorrelationIdHeader, CorrelationIdThreadStorageHolder.CorrelationId }
        };

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(metadata));

        return continuation(request, newContext);
    }
}
