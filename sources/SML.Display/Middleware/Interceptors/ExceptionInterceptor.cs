namespace SML.Display.Middleware.Interceptors;

using Core.Data.Settings;
using Core.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using SML.Shared.Exception;
using System.Text.Json;

public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;

    private readonly string _serviceName;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger, IOptions<GeneralSettings> settings)
    {
        _logger = logger;
        _serviceName = settings.Value.ServiceName;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        LogRequest(context, request);
        //TODO check if logging of parameters is allowed (does not contain sensitive data). If not, remove the {@HttpRequestParameters} argument from each log call
        try
        {
            return await continuation(request, context);
        }
        catch (SmlException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}", context.GetHttpContext().Request.Path, request);
            throw CreateRpcException(StatusCode.FailedPrecondition, new SmlException(e.Type, e.Message, e.Placeholders));
        }
        catch (RpcException e) when (e.Source == "Calzolari.Grpc.AspNetCore.Validation")
        {
            _logger.LogError(e, "Call of {HttpRequestPath} with invalid parameters {@HttpRequestParameters}, {ExceptionMessage}", context.GetHttpContext().Request.Path, request, e.Message);
            throw CreateRpcException(e.Status.StatusCode, new SmlException(ExceptionType.Unknown, e.Message));
        }
        catch (RpcException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}", context.GetHttpContext().Request.Path, request);
            throw CreateRpcException(e.Status.StatusCode, new SmlException(ExceptionType.Unknown, e.Message));
        }
        //TODO Add any custom exception catch if required
        catch (AlreadyExistException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}, entity already exists. {ExceptionMessage}", context.GetHttpContext().Request.Path, request, e.Message);
            var exception = new SmlException(ExceptionType.AlreadyExists, e.Message);
            if (e.AlreadyExistsElement is { } element)
            {
                exception.Placeholders.Add("display_name", element);
            }
            throw CreateRpcException(StatusCode.AlreadyExists, exception);
        }
        catch (NotFoundException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}, entity not found. {ExceptionMessage}", context.GetHttpContext().Request.Path, request, e.Message);
            var exception = new SmlException(ExceptionType.NotFound, e.Message);
            if (e.NotFoundElement is { } element)
            {
                exception.Placeholders.Add("display_name", element);
            }
            throw CreateRpcException(StatusCode.NotFound, exception);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}, operation is not valid. {ExceptionMessage}", context.GetHttpContext().Request.Path, request, e.Message);
            throw CreateRpcException(StatusCode.FailedPrecondition, new SmlException(ExceptionType.Unknown, e.Message));
        }
        catch (NotImplementedException e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}, method not implemented. {ExceptionMessage}", context.GetHttpContext().Request.Path, request, e.Message);
            throw CreateRpcException(StatusCode.Unimplemented, new SmlException(ExceptionType.Unknown, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while calling {HttpRequestPath} with {@HttpRequestParameters}", context.GetHttpContext().Request.Path, request);
            throw CreateRpcException(StatusCode.FailedPrecondition, new SmlException(ExceptionType.Unknown, e.Message));
        }
    }

    private void LogRequest(ServerCallContext context, object request)
    {
        //TODO check if logging of parameters is allowed (does not contain sensitive data). Remove the {@HttpRequestParameters} argument if not
        _logger.LogDebug("Starting call {HttpRequestPath} with {@HttpRequestParameters}", context.GetHttpContext().Request.Path, request);
    }

    private RpcException CreateRpcException(StatusCode statusCode, SmlException exception)
    {
        exception.ServiceName = _serviceName;
        var json = JsonSerializer.Serialize(exception);
        var status = new Status(statusCode, json);
        var trailers = new Metadata { { "exception-class-name", typeof(SmlException).FullName! } };
        return new RpcException(status, trailers, exception.Message);
    }
}
