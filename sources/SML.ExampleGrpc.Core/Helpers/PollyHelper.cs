namespace SML.ExampleGrpc.Core.Helpers;

using Core.Data.Settings;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Wrap;
using System;

public static class PollyHelper
{
    private const string ContextRetryKey = "retry";    
    private const string RetryMessage = "Attempt {retry_attempt} failed. Retry in {sleep_duration} seconds.";

    public static AsyncPolicyWrap CreateRetryPolicy(ILogger logger, BackoffSettings backoffSettings, BreakerSettings breakerSettings)
        => Policy.WrapAsync(
                Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: breakerSettings.FailuresBeforeBreaking,
                    durationOfBreak: breakerSettings.BreakDuration,
                    onBreak: (e, breakDelay) => OnBreak(logger, e, breakDelay),
                    onReset: () => OnReset(logger),
                    onHalfOpen: () => OnHalfOpen(logger)),
                CreateRetryPolicy(logger, backoffSettings));

    public static AsyncRetryPolicy CreateRetryPolicy(ILogger logger, BackoffSettings backoffSettings)
        => Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: backoffSettings.MaxAttempts - 1,
                sleepDurationProvider: retryAttempt =>
                {
                    var backoff = backoffSettings.InitialBackoff * Math.Pow(backoffSettings.BackoffMultiplier, retryAttempt - 1);
                    if (backoff > backoffSettings.MaxBackoff)
                    {
                        return backoffSettings.MaxBackoff;
                    }
                    return backoff;
                },
                onRetry: (e, timeSpan, retryAttempt, context) => OnRetry(logger, e, timeSpan, retryAttempt, context));

    public static int? GetRetries(Context context)
        => context.TryGetValue(ContextRetryKey, out var value)
            ? value is int retries ? retries : null
            : null;

    private static void OnBreak(ILogger logger, Exception e, TimeSpan breakDelay)
    {
        logger.LogError(e, "Circuit opened for {PollyBreakDelay} seconds: No attempt allowed", breakDelay.TotalSeconds);
    }

    private static void OnReset(ILogger logger)
    {
        logger.LogInformation("Circuit closed: Attempts can be made again");
    }

    private static void OnHalfOpen(ILogger logger)
    {
        logger.LogWarning("Circuit is half-open: A single attempt will be tried");
    }

    private static void OnRetry(ILogger logger, Exception e, TimeSpan timeSpan, int retryAttempt, Context context)
    {
        context[ContextRetryKey] = retryAttempt;
        if (retryAttempt > 1)
        {
            logger.LogDebug(e, RetryMessage, retryAttempt, timeSpan.TotalSeconds);
        }
        else
        {
            logger.LogError(e, RetryMessage, retryAttempt, timeSpan.TotalSeconds);
        }
    }
}
