using Microsoft.Extensions.Logging;
using Soenneker.Utils.Random;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.Delay;

/// <summary>
/// A utility library for generic time delay related operations
/// </summary>
public static class DelayUtil
{
    private const double _msToSeconds = 0.001d;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask CompletedOrCanceled(CancellationToken ct) => ct.IsCancellationRequested ? ValueTask.FromCanceled(ct) : ValueTask.CompletedTask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ClampMin1(int value) => value <= 0 ? 1 : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ClampNonNegative(int value) => value <= 0 ? 0 : value;

    /// <summary>
    /// Asynchronously delays execution for the specified time.
    /// </summary>
    public static ValueTask Delay(int milliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger != null)
            DelayLog.DelayingSeconds(logger, milliseconds * _msToSeconds);

        if (milliseconds <= 0)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(milliseconds, cancellationToken));
    }

    /// <summary>
    /// Asynchronously delays execution for the specified timespan.
    /// </summary>
    public static ValueTask Delay(TimeSpan delay, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger != null)
            DelayLog.DelayingSeconds(logger, delay.TotalSeconds);

        if (delay <= TimeSpan.Zero)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(delay, cancellationToken));
    }

    /// <summary>
    /// Blocks the calling thread for the specified number of milliseconds, optionally logging the delay using the
    /// provided logger.
    /// </summary>
    /// <remarks>This method performs a synchronous, blocking delay using <see cref="Thread.Sleep"/>. If a
    /// logger is provided, the delay duration is logged before blocking. Use with caution in performance-sensitive or
    /// UI contexts, as blocking the thread may impact responsiveness.</remarks>
    /// <param name="milliseconds">The number of milliseconds for which the thread is blocked. Must be zero or greater.</param>
    /// <param name="logger">An optional logger used to record the blocking delay. If null, no logging is performed.</param>
    public static void DelaySync(int milliseconds, ILogger? logger = null)
    {
        if (logger != null)
            DelayLog.BlockingSeconds(logger, milliseconds * _msToSeconds);

        if (milliseconds > 0)
            Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Asynchronously delays execution with a random jitter to prevent synchronized waits.
    /// </summary>
    public static ValueTask DelayWithJitter(int baseMilliseconds, ILogger? logger = null, double jitterFactor = 0.5,
        CancellationToken cancellationToken = default)
    {
        if (baseMilliseconds <= 0 || jitterFactor <= 0)
        {
            if (logger != null)
                DelayLog.DelayingSeconds(logger, baseMilliseconds * _msToSeconds);

            return baseMilliseconds <= 0 ? CompletedOrCanceled(cancellationToken) : new ValueTask(Task.Delay(baseMilliseconds, cancellationToken));
        }

        if (jitterFactor > 1d)
            jitterFactor = 1d;

        var jitterMs = (int)(RandomUtil.NextDouble() * (baseMilliseconds * jitterFactor));
        int finalDelay = baseMilliseconds + jitterMs;

        if (logger != null)
            DelayLog.DelayingWithJitterSeconds(logger, finalDelay * _msToSeconds);

        return new ValueTask(Task.Delay(finalDelay, cancellationToken));
    }

    /// <summary>
    /// Asynchronously delays execution using an integer-based exponential backoff strategy.
    /// </summary>
    public static ValueTask DelayWithBackoff(int attempt, ILogger? logger = null, int baseDelayMs = 1000, int maxDelayMs = 30000,
        CancellationToken cancellationToken = default)
    {
        baseDelayMs = ClampMin1(baseDelayMs);
        maxDelayMs = ClampMin1(maxDelayMs);

        int delay;

        if (attempt <= 0)
        {
            delay = baseDelayMs <= maxDelayMs ? baseDelayMs : maxDelayMs;
        }
        else if (attempt >= 30)
        {
            delay = maxDelayMs;
        }
        else
        {
            int shifted = baseDelayMs << attempt;
            delay = shifted >= maxDelayMs ? maxDelayMs : shifted;
        }

        if (logger != null)
            DelayLog.BackoffDelay(logger, delay * _msToSeconds, attempt);

        if (delay <= 0)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(delay, cancellationToken));
    }

    /// <summary>
    /// Asynchronously delays execution for the specified number of seconds (fractional allowed).
    /// </summary>
    public static ValueTask DelaySeconds(double seconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (logger != null)
            DelayLog.DelayingSeconds(logger, seconds);

        if (seconds <= 0d)
            return CompletedOrCanceled(cancellationToken);

        double msDouble = seconds * 1000d;
        int ms = msDouble >= int.MaxValue ? int.MaxValue : (int)msDouble;

        if (ms <= 0)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(ms, cancellationToken));
    }

    /// <summary>
    /// Asynchronously delays until the specified UTC DateTimeOffset.
    /// If targetUtc ≤ now, returns immediately.
    /// </summary>
    public static ValueTask DelayUntil(DateTimeOffset target, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (target <= now)
        {
            if (logger != null)
                DelayLog.TargetAlreadyPassed(logger);

            return CompletedOrCanceled(cancellationToken);
        }

        TimeSpan toWait = target - now;

        if (logger != null)
            DelayLog.DelayingUntil(logger, target, toWait.TotalSeconds);

        if (toWait <= TimeSpan.Zero)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(toWait, cancellationToken));
    }

    /// <summary>
    /// Asynchronously delays execution for a random duration between minMs and maxMs.
    /// </summary>
    public static ValueTask DelayRandomRange(int minMilliseconds, int maxMilliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        // normalize negatives to 0ms
        minMilliseconds = ClampNonNegative(minMilliseconds);

        int span = maxMilliseconds - minMilliseconds;

        int finalDelay;
        if (span <= 0)
        {
            finalDelay = minMilliseconds;
        }
        else
        {
            int jitter = RandomUtil.Next(span + 1);
            finalDelay = minMilliseconds + jitter;
        }

        if (logger != null)
            DelayLog.DelayingRandomSeconds(logger, finalDelay * _msToSeconds);

        if (finalDelay <= 0)
            return CompletedOrCanceled(cancellationToken);

        return new ValueTask(Task.Delay(finalDelay, cancellationToken));
    }
}