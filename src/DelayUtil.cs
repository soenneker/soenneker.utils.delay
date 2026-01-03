using Microsoft.Extensions.Logging;
using Soenneker.Utils.Random;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.Delay;

/// <summary>
/// A utility library for generic time delay related operations
/// </summary>
public static class DelayUtil
{
    private const double _msToSeconds = 1d / 1000d;

    /// <summary>
    /// Asynchronously delays execution for the specified time.
    /// </summary>
    public static Task Delay(int milliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger is not null)
            DelayLog.DelayingSeconds(logger, milliseconds * _msToSeconds);

        return Task.Delay(milliseconds, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified timespan.
    /// </summary>
    public static Task Delay(TimeSpan delay, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger is not null)
            DelayLog.DelayingSeconds(logger, delay.TotalSeconds);

        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Blocks execution synchronously for the specified number of milliseconds.
    /// </summary>
    public static void DelaySync(int milliseconds, ILogger? logger = null)
    {
        if (logger is not null)
            DelayLog.BlockingSeconds(logger, milliseconds * _msToSeconds);

        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Asynchronously delays execution with a random jitter to prevent synchronized waits.
    /// </summary>
    public static Task DelayWithJitter(int baseMilliseconds, ILogger? logger = null, double jitterFactor = 0.5, CancellationToken cancellationToken = default)
    {
        if (baseMilliseconds <= 0 || jitterFactor <= 0)
        {
            if (logger is not null)
                DelayLog.DelayingSeconds(logger, baseMilliseconds * _msToSeconds);

            return Task.Delay(baseMilliseconds, cancellationToken);
        }

        if (jitterFactor > 1)
            jitterFactor = 1;

        // jitterMs = random[0,1) * baseMs * jitterFactor (use double once; avoid overflow by staying in double)
        int jitterMs = (int)(RandomUtil.NextDouble() * (baseMilliseconds * jitterFactor));
        int finalDelay = baseMilliseconds + jitterMs;

        if (logger is not null)
            DelayLog.DelayingWithJitterSeconds(logger, finalDelay * _msToSeconds);

        return Task.Delay(finalDelay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution using an integer-based exponential backoff strategy.
    /// </summary>
    public static Task DelayWithBackoff(int attempt, ILogger? logger = null, int baseDelayMs = 1000, int maxDelayMs = 30000,
        CancellationToken cancellationToken = default)
    {
        if (baseDelayMs <= 0)
            baseDelayMs = 1;

        if (maxDelayMs <= 0)
            maxDelayMs = 1;

        if (attempt <= 0)
        {
            int d0 = baseDelayMs <= maxDelayMs ? baseDelayMs : maxDelayMs;

            if (logger is not null)
                DelayLog.BackoffDelay(logger, d0 * _msToSeconds, attempt);

            return Task.Delay(d0, cancellationToken);
        }

        // Guard against shift masking (attempt is effectively mod 64 for long shifts)
        long delayLong = attempt >= 31 ? long.MaxValue : ((long)baseDelayMs << attempt);
        if (delayLong > maxDelayMs)
            delayLong = maxDelayMs;

        int delay = (int)delayLong;

        if (logger is not null)
            DelayLog.BackoffDelay(logger, delay * _msToSeconds, attempt);

        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified number of seconds (fractional allowed).
    /// </summary>
    public static Task DelaySeconds(double seconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger is not null)
            DelayLog.DelayingSeconds(logger, seconds);

        if (seconds <= 0)
            return Task.CompletedTask;

        double msDouble = seconds * 1000d;
        int ms = msDouble >= int.MaxValue ? int.MaxValue : (int)msDouble;

        return Task.Delay(ms, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays until the specified UTC DateTimeOffset.
    /// If targetUtc ≤ now, returns immediately.
    /// </summary>
    public static Task DelayUntil(DateTimeOffset target, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (target <= now)
        {
            if (logger is not null)
                DelayLog.TargetAlreadyPassed(logger);

            return Task.CompletedTask;
        }

        TimeSpan toWait = target - now;

        if (logger is not null)
            DelayLog.DelayingUntil(logger, target, toWait.TotalSeconds);

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
            if (logger is not null)
                DelayLog.DelayingRandomSeconds(logger, minMilliseconds * _msToSeconds);

            return Task.Delay(minMilliseconds, cancellationToken);
        }

        int jitter = RandomUtil.Next(span + 1);
        int finalDelay = minMilliseconds + jitter;

        if (logger is not null)
            DelayLog.DelayingRandomSeconds(logger, finalDelay * _msToSeconds);

        return Task.Delay(finalDelay, cancellationToken);
    }
}