namespace SML.Display.Core.Correlations.HttpDelegatingHandlers;

using Core.Correlations.ThreadStorages;

public class HttpCorrelationHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(CorrelationIdThreadStorageHolder.CorrelationIdHeader, CorrelationIdThreadStorageHolder.CorrelationId);

        return await base.SendAsync(request, cancellationToken);
    }
}
