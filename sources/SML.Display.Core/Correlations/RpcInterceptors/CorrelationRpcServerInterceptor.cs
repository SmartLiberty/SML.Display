namespace SML.Display.Core.Correlations.RpcInterceptors;

using Core.Correlations.ThreadStorages;
using Grpc.Core;
using Grpc.Core.Interceptors;

public class CorrelationRpcServerInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> next)
    {
        var correlationIdEntry = context.RequestHeaders.Get(CorrelationIdThreadStorageHolder.CorrelationIdHeader);
        if (correlationIdEntry != null)
        {
            CorrelationIdThreadStorageHolder.CorrelationId = correlationIdEntry.Value;
        }

        using (CorrelationIdThreadStorageHolder.PushCorrelationContext())
        {
            context.ResponseTrailers.Add(CorrelationIdThreadStorageHolder.CorrelationIdHeader, CorrelationIdThreadStorageHolder.CorrelationId);

            return await next(request, context);
        }
    }
}
