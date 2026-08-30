[![](https://img.shields.io/nuget/v/soenneker.utils.delay.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.delay/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.delay/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.delay/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.delay.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.delay/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.delay/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.delay/actions/workflows/codeql.yml)

# Soenneker.Utils.Delay

Static delay helpers for cancellable waits, positive jitter, exponential backoff, random ranges, and UTC deadlines.

## Installation

```bash
dotnet add package Soenneker.Utils.Delay
```

## Basic delays

```csharp
await DelayUtil.Delay(250, cancellationToken: cancellationToken);
await DelayUtil.Delay(TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);
await DelayUtil.DelaySeconds(1.5, cancellationToken: cancellationToken);
await DelayUtil.DelayUntil(deadlineUtc, cancellationToken: cancellationToken);
```

Non-positive durations and deadlines that have passed complete immediately. They still produce a cancelled `ValueTask` when the supplied token is already cancelled.

`DelaySync()` uses `Thread.Sleep` and blocks the calling thread. Reserve it for code that cannot be asynchronous; do not use it on request, UI, or other throughput-sensitive threads.

## Jitter and backoff

```csharp
await DelayUtil.DelayWithJitter(
    baseMilliseconds: 1_000,
    jitterFactor: 0.25,
    cancellationToken: cancellationToken);

await DelayUtil.DelayWithBackoff(
    attempt: attempt,
    baseDelayMs: 500,
    maxDelayMs: 30_000,
    cancellationToken: cancellationToken);
```

Jitter is positive-only: a factor of `0.25` produces a wait from the base delay up to, but generally below, 125% of it. Factors above `1` are capped at `1`; non-positive or `NaN` factors use the base delay without jitter.

Backoff uses `baseDelayMs * 2^attempt`, capped by `maxDelayMs`. Attempts at or below zero use the base delay, attempts at 30 or above use the maximum, and non-positive base or maximum values are normalized to one millisecond.

## Random range

```csharp
await DelayUtil.DelayRandomRange(
    minMilliseconds: 200,
    maxMilliseconds: 500,
    cancellationToken: cancellationToken);
```

Both endpoints are inclusive. A negative minimum is normalized to zero. If the maximum is not greater than the normalized minimum, the minimum is used.

Every helper accepts an optional `ILogger`; when supplied, the selected delay is logged before waiting.
