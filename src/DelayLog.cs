using System;
using Microsoft.Extensions.Logging;

namespace Soenneker.Utils.Delay;

internal static partial class DelayLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Delaying for {time}s...")]
    public static partial void DelayingSeconds(ILogger logger, double time);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Blocking for {time}s...")]
    public static partial void BlockingSeconds(ILogger logger, double time);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Delaying with jitter for {time}s...")]
    public static partial void DelayingWithJitterSeconds(ILogger logger, double time);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Exponential backoff delay for {time}s (attempt {attempt})...")]
    public static partial void BackoffDelay(ILogger logger, double time, int attempt);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Delaying until {time} UTC (~{seconds}s)...")]
    public static partial void DelayingUntil(ILogger logger, DateTime time, double seconds);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Target time already passed; returning immediately.")]
    public static partial void TargetAlreadyPassed(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Delaying randomly for {time}s...")]
    public static partial void DelayingRandomSeconds(ILogger logger, double time);
}