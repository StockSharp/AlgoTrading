# Time Sync FORTS Utility
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy mirrors the MetaTrader 5 expert advisor **Time_sync_forts.mq5**. It does not place any orders. Instead, it listens to
FORTS trade ticks and aligns the local Windows system clock with the exchange server time during the narrow technical windows used
by the original script.

## Purpose

- Poll the derivative market (FORTS) tick stream to obtain the current exchange time.
- During each maintenance window call the Win32 `SetLocalTime` API so that the workstation clock matches the exchange clock.
- Prevent repeated adjustments until the next synchronization window opens.

## Synchronization Windows

The expert synchronizes only during the same maintenance slots as the MQL program (Moscow exchange time):

1. 09:45–10:00
2. 10:01–10:02
3. 13:58–14:00
4. 14:05–14:06
5. 18:43–18:45
6. 19:05–19:06
7. 23:48–23:50

Outside of these windows the strategy simply waits and resets the internal flag so that the next window triggers another update.

## Parameters

- **FirstSkippedDay** – weekday that must be ignored (default Saturday). When the server time falls on this day the strategy only
  resets its internal flag.
- **SecondSkippedDay** – additional weekday to skip (default Sunday). Matches the original weekend exclusion.
- **LatencyCompensationMilliseconds** – additional milliseconds added to the server timestamp before calling `SetLocalTime`.
  The MQL script estimated the network delay using MetaTrader ping. You can emulate the same offset here (positive or negative) to
  fine-tune the resulting system time.

All parameters are created with `StrategyParam` so they can be exposed in the StockSharp UI and optimized if necessary.

## Data Subscription

The strategy declares a single working data set `(Security, DataType.Ticks)` and subscribes to trades through the high-level API.
Every tick delivers an `ExecutionMessage` with a server timestamp, which becomes the basis for the synchronization logic. When a
tick arrives inside an allowed window the strategy performs one synchronization attempt and logs the outcome.

## Platform Requirements

- Windows operating system. The Win32 `kernel32.dll` entry point is required; on other platforms the strategy logs a warning and
  skips the time adjustment.
- Administrator privileges may be necessary because changing the system clock is restricted on many corporate machines.
- Assign the strategy to the same FORTS instrument that you would use inside MetaTrader so the received tick timestamps represent
the exchange time.

## Implementation Notes

- `SetLocalTime` is wrapped through P/Invoke and protected with bounds checks on the `SYSTEMTIME` structure.
- The logic keeps an `_isSyncCompleted` flag to avoid repeated adjustments within the same window. The flag is cleared when the
  session leaves the window or when the skipped weekdays are detected.
- Extensive logging was added to show the synchronized time, the originating server time, and potential Win32 error codes when the
  system call fails.
