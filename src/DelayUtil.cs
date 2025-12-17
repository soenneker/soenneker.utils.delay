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
    private const double _msToSeconds = 1d / 1000d;

    // Precompiled logging delegates to avoid params object[] allocations + boxing.
    private static readonly Action<ILogger, double, Exception?> _logDelaySeconds =
        LoggerMessage.Define<double>(LogLevel.Information, new EventId(1, nameof(Delay)), "Delaying for {time}s...");

    private static readonly Action<ILogger, double, int, Exception?> _logBackoffSecondsAttempt = LoggerMessage.Define<double, int>(LogLevel.Information,
        new EventId(2, nameof(DelayWithBackoff)), "Exponential backoff delay for {time}s (attempt {attempt})...");

    private static readonly Action<ILogger, DateTime, double, Exception?> _logDelayUntil =
        LoggerMessage.Define<DateTime, double>(LogLevel.Information, new EventId(3, nameof(DelayUntil)), "Delaying until {time} UTC (~{seconds}s)...");

    private static readonly Action<ILogger, Exception?> _logTargetAlreadyPassed = LoggerMessage.Define(LogLevel.Information, new EventId(4, nameof(DelayUntil)),
        "Target time already passed; returning immediately.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogDelaySeconds(ILogger? logger, double seconds)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
            return;

        _logDelaySeconds(logger, seconds, null);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified time.
    /// </summary>
    public static Task Delay(int milliseconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        // Task.Delay(int, token) only accepts int; negative/zero completes immediately.
        LogDelaySeconds(logger, milliseconds * _msToSeconds);
        return Task.Delay(milliseconds, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified timespan.
    /// </summary>
    public static Task Delay(TimeSpan delay, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        // Prefer the TimeSpan overload; it handles large values safely.
        LogDelaySeconds(logger, delay.TotalSeconds);
        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Blocks execution synchronously for the specified number of milliseconds.
    /// </summary>
    public static void DelaySync(int milliseconds, ILogger? logger = null)
    {
        LogDelaySeconds(logger, milliseconds * _msToSeconds);
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Asynchronously delays execution with a random jitter to prevent synchronized waits.
    /// </summary>
    public static Task DelayWithJitter(int baseMilliseconds, ILogger? logger = null, double jitterFactor = 0.5, CancellationToken cancellationToken = default)
    {
        // jitterFactor ∈ [0,1] (we’ll tolerate outside values by clamping).
        if (jitterFactor <= 0 || baseMilliseconds <= 0)
        {
            LogDelaySeconds(logger, baseMilliseconds * _msToSeconds);
            return Task.Delay(baseMilliseconds, cancellationToken);
        }

        if (jitterFactor > 1)
            jitterFactor = 1;

        // Use long to avoid overflow on multiplication.
        // jitterMs = random[0,1) * baseMs * jitterFactor
        var jitterMs = (int)(RandomUtil.NextDouble() * (baseMilliseconds * jitterFactor));
        int finalDelay = baseMilliseconds + jitterMs;

        LogDelaySeconds(logger, finalDelay * _msToSeconds);
        return Task.Delay(finalDelay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution using an integer-based exponential backoff strategy.
    /// </summary>
    public static Task DelayWithBackoff(int attempt, ILogger? logger = null, int baseDelayMs = 1000, int maxDelayMs = 30000,
        CancellationToken cancellationToken = default)
    {
        if (attempt <= 0)
        {
            int d0 = baseDelayMs <= maxDelayMs ? baseDelayMs : maxDelayMs;
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
                _logBackoffSecondsAttempt(logger, d0 * _msToSeconds, attempt, null);

            return Task.Delay(d0, cancellationToken);
        }

        if (baseDelayMs <= 0)
            baseDelayMs = 1;

        if (maxDelayMs <= 0)
            maxDelayMs = 1;

        // Saturating left shift using long to avoid overflow.
        // delay = min(maxDelayMs, baseDelayMs * 2^attempt)
        long delayLong = (long)baseDelayMs << attempt;
        if (delayLong > maxDelayMs)
            delayLong = maxDelayMs;

        int delay = (int)delayLong;

        if (logger is not null && logger.IsEnabled(LogLevel.Information))
            _logBackoffSecondsAttempt(logger, delay * _msToSeconds, attempt, null);

        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Asynchronously delays execution for the specified number of seconds (fractional allowed).
    /// </summary>
    public static Task DelaySeconds(double seconds, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (seconds <= 0)
        {
            LogDelaySeconds(logger, seconds);
            return Task.CompletedTask;
        }

        // Convert safely; Task.Delay(int) caps at int.MaxValue ms.
        double msDouble = seconds * 1000d;
        int ms = msDouble >= int.MaxValue ? int.MaxValue : (int)msDouble;

        LogDelaySeconds(logger, seconds);
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
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
                _logTargetAlreadyPassed(logger, null);

            return Task.CompletedTask;
        }

        TimeSpan toWait = targetUtc - now;

        if (logger is not null && logger.IsEnabled(LogLevel.Information))
            _logDelayUntil(logger, targetUtc, toWait.TotalSeconds, null);

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
            LogDelaySeconds(logger, minMilliseconds * _msToSeconds);
            return Task.Delay(minMilliseconds, cancellationToken);
        }

        int jitter = RandomUtil.Next(span + 1);
        int finalDelay = minMilliseconds + jitter;

        LogDelaySeconds(logger, finalDelay * _msToSeconds);
        return Task.Delay(finalDelay, cancellationToken);
    }
}