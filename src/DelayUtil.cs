using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Utils.Random;

namespace Soenneker.Utils.Delay;

/// <summary>
/// A utility library for generic time delay related operations
/// </summary>
public static class DelayUtil
{
    /// <summary>
    /// Asynchronously delays execution for the specified time.
    /// </summary>
    public static Task Delay(int milliseconds, ILogger? logger, CancellationToken cancellationToken = default)
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
        logger?.LogInformation("Delaying for {time}s...", timeSpan.TotalSeconds);
        return Task.Delay(timeSpan, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified timespan.
    /// </summary>
    public static Task Delay(TimeSpan delay, ILogger? logger, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Delaying for {time}s...", delay.TotalSeconds);
        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Blocks execution synchronously for the specified duration (not recommended for UI).
    /// </summary>
    public static void DelaySync(int milliseconds, ILogger? logger)
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
        logger?.LogInformation("Blocking for {time}s...", timeSpan.TotalSeconds);
        Thread.Sleep(timeSpan);
    }

    /// <summary>
    /// Asynchronously delays execution with a random jitter to prevent synchronized waits.
    /// </summary>
    public static Task DelayWithJitter(int baseMilliseconds, ILogger? logger, double jitterFactor = 0.5, CancellationToken cancellationToken = default)
    {
        double jitter = RandomUtil.NextDouble() * jitterFactor * baseMilliseconds;
        int finalDelay = baseMilliseconds + (int)jitter;

        logger?.LogInformation("Delaying with jitter for {time}s...", TimeSpan.FromMilliseconds(finalDelay).TotalSeconds);
        return Task.Delay(finalDelay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution using an exponential backoff strategy, useful for retries.
    /// </summary>
    public static Task DelayWithBackoff(int attempt, ILogger? logger, int baseDelayMs = 1000, int maxDelayMs = 30000, CancellationToken cancellationToken = default)
    {
        int delay = Math.Min(baseDelayMs * (int)Math.Pow(2, attempt), maxDelayMs);

        logger?.LogInformation("Exponential backoff delay for {time}s (attempt {attempt})...", TimeSpan.FromMilliseconds(delay).TotalSeconds, attempt);
        return Task.Delay(delay, cancellationToken);
    }
}
