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
    public static Task Delay(int milliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Delaying for {time}s...", milliseconds / 1000.0);
        return Task.Delay(milliseconds, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified timespan.
    /// </summary>
    public static Task Delay(TimeSpan delay, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Delaying for {time}s...", delay.TotalSeconds);
        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Blocks execution synchronously for the specified number of milliseconds.
    /// </summary>
    public static void DelaySync(int milliseconds, ILogger? logger = null)
    {
        logger?.LogInformation("Blocking for {time}s...", milliseconds / 1000.0);
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Asynchronously delays execution with a random jitter to prevent synchronized waits.
    /// </summary>
    public static Task DelayWithJitter(int baseMilliseconds, ILogger? logger = null, double jitterFactor = 0.5, CancellationToken cancellationToken = default)
    {
        // jitterFactor ∈ [0,1]: jitterMs = random * baseMs * jitterFactor
        double jitter = RandomUtil.NextDouble() * baseMilliseconds * jitterFactor;
        int finalDelay = baseMilliseconds + (int) jitter;

        logger?.LogInformation("Delaying with jitter for {time}s...", finalDelay / 1000.0);

        return Task.Delay(finalDelay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution using an integer‐based exponential backoff strategy.
    /// </summary>
    public static Task DelayWithBackoff(int attempt, ILogger? logger = null, int baseDelayMs = 1000, int maxDelayMs = 30000,
        CancellationToken cancellationToken = default)
    {
        // Compute baseDelayMs * 2^attempt without Math.Pow (all ints).
        int delay = baseDelayMs;
        for (var i = 0; i < attempt; i++)
        {
            // If shifting would exceed maxDelayMs, clamp and break.
            if (delay >= (maxDelayMs >> 1))
            {
                delay = maxDelayMs;
                break;
            }

            delay <<= 1; // multiply by 2
        }

        if (delay > maxDelayMs)
            delay = maxDelayMs;

        logger?.LogInformation("Exponential backoff delay for {time}s (attempt {attempt})...", delay / 1000.0, attempt);

        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified number of seconds (fractional allowed).
    /// </summary>
    public static Task DelaySeconds(double seconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var ms = (int) (seconds * 1000);
        logger?.LogInformation("Delaying for {time}s...", seconds);
        return Task.Delay(ms, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays until the specified UTC DateTime.
    /// If targetUtc ≤ now, returns immediately.
    /// </summary>
    public static Task DelayUntil(DateTime targetUtc, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        if (targetUtc <= now)
        {
            logger?.LogInformation("Target time already passed; returning immediately.");
            return Task.CompletedTask;
        }

        TimeSpan toWait = targetUtc - now;
        logger?.LogInformation("Delaying until {time} UTC (~{seconds}s)...", targetUtc, toWait.TotalSeconds);
        return Task.Delay(toWait, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for a random duration between minMs and maxMs.
    /// </summary>
    public static Task DelayRandomRange(int minMilliseconds, int maxMilliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        int span = maxMilliseconds - minMilliseconds;

        if (span <= 0)
        {
            logger?.LogInformation("Delaying for {time}s...", minMilliseconds / 1000.0);
            return Task.Delay(minMilliseconds, cancellationToken);
        }

        int jitter = RandomUtil.Next(span + 1);
        int finalDelay = minMilliseconds + jitter;
        logger?.LogInformation("Delaying randomly for {time}s...", finalDelay / 1000.0);
        return Task.Delay(finalDelay, cancellationToken);
    }
}