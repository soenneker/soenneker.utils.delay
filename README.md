[![](https://img.shields.io/nuget/v/soenneker.utils.delay.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.delay/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.delay/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.delay/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.delay.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.delay/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.delay/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.delay/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.Delay
A utility library for generic time delay related operations.

## Installation

```bash
dotnet add package Soenneker.Utils.Delay
```

## Quick start

```csharp
using Soenneker.Utils.Delay;
```

Call the static `DelayUtil` methods directly; no dependency-injection registration is required.

## Common operations

- `Delay()` - Asynchronously delays execution for the specified time.
- `DelaySync()` - Blocks the calling thread for the specified number of milliseconds, optionally logging the delay using the provided logger. This method performs a synchronous, blocking delay using `Thread.Sleep`.
- `DelayWithJitter()` - Waits for the base delay plus a random positive jitter. The jitter factor is capped at `1.0`; non-positive delays complete immediately.
- `DelayWithBackoff()` - Applies exponential backoff for the attempt number, constrained by the configured base and maximum delays.
- `DelaySeconds()` - Accepts fractional seconds and converts them to an asynchronous millisecond delay; non-positive values complete immediately.
- `DelayUntil()` - Waits until a UTC `DateTimeOffset`; a target that has already passed completes immediately.
- `DelayRandomRange()` - Chooses an inclusive random delay between the supplied minimum and maximum millisecond values.
