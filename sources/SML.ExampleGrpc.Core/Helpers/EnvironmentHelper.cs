using Serilog;
using Serilog.Core;

namespace SML.ExampleGrpc.Core.Helpers;

using Microsoft.Extensions.Logging;

public static class EnvironmentHelper
{
    public static void FailFast<T>(ILogger<T> logger, string? message = null, Exception? exception = null)
    {
        if (exception is TaskCanceledException)
        {
            logger.LogTrace("Do not fail on TaskCanceledException");
            return;
        }
        logger.LogCritical(exception, "Application shutdown: {FatalMessage}", message);
        FailFast(message, exception);
    }

    public static void FailFast(Serilog.ILogger logger, string? message = null, Exception? exception = null)
    {
        if (exception is TaskCanceledException)
        {
            logger.Verbose("Do not fail on TaskCanceledException");
            return;
        }
        logger.Fatal(exception, "Application shutdown: {FatalMessage}", message);
        FailFast(message, exception);
    }

    private static void FailFast(string? message = null, Exception? exception = null)
    {
        Log.CloseAndFlush();
        Environment.FailFast(message, exception);
    }
}
