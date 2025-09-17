# News Filter Strategy (MQL Conversion)

## Overview

The **News Filter Strategy** is a direct conversion of the "NEWS_Filter" MetaTrader 4 expert advisor. The strategy periodically downloads the upcoming economic calendar from the FXStreet service and evaluates whether trading should be paused before and after important macroeconomic events. It keeps the same configuration philosophy as the original script while using the StockSharp high-level strategy API.

## Core Idea

1. Fetch the economic calendar for the next seven days from the FXStreet mini widget endpoint.
2. Parse each scheduled event, extracting date, time, currency, importance (volatility), and descriptive text.
3. Apply filters for:
   - Currency codes defined by the user.
   - Importance levels (low, medium, high).
   - Optional keyword that must appear in the event title.
4. Maintain a rolling list of future events and determine whether the current time falls into the configurable "do not trade" window around any of them.
5. Expose the `IsNewsActive` flag and log informative status messages so the host application can suspend order generation while the flag is `true`.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `EnableNewsFilter` | `true` | Master switch that enables news download and filtering logic. |
| `UseLowImportance` | `false` | Allow events marked with one star (low impact). |
| `UseMediumImportance` | `true` | Allow events marked with two stars (medium impact). |
| `UseHighImportance` | `true` | Allow events marked with three stars (high impact). |
| `StopBeforeNewsMinutes` | `30` | Minutes before a matching event when trading should be halted. |
| `StartAfterNewsMinutes` | `30` | Minutes after a matching event when trading resumes. |
| `CurrenciesFilter` | `USD,EUR,CAD,AUD,NZD,GBP` | Comma separated list of currencies that should trigger the filter. |
| `FilterByKeyword` | `false` | Require event titles to contain the configured keyword. |
| `Keyword` | `employment` | Keyword to search for when `FilterByKeyword` is enabled. |
| `RefreshIntervalMinutes` | `10` | Minutes between calendar reloads. The timer checks the cache every minute and refreshes when it becomes stale. |

## Trading Logic

- A single timer is responsible for refreshing the calendar cache and for checking whether the current time intersects with any event window.
- The event window is defined as `[event time - StopBeforeNewsMinutes, event time + StartAfterNewsMinutes]`.
- If the current time is inside any event window, the strategy sets `IsNewsActive = true` and logs a message similar to `News time: USD Nonfarm Payrolls at 2024-06-07 12:30`.
- Outside of the window the strategy logs the next upcoming event (if any) and resets `IsNewsActive = false`.
- The host strategy or application should consult `IsNewsActive` before submitting new orders, mimicking the original EA behaviour that used the global `NEWS_ON` flag and the `Comment()` function.

## Porting Notes

- The HTML parsing logic mirrors the original string operations but is wrapped in resilient regular expressions and also supports a JSON payload if the provider ever changes format.
- Chart labels and graphical vertical lines from the MQL version are not recreated because StockSharp strategies typically rely on external UI tooling. Instead, detailed log messages are produced.
- Time zone handling assumes the FXStreet endpoint publishes UTC timestamps, matching the EA that corrected times using `TimeGMTOffset()`.
- The strategy is intentionally passive: it does not submit orders by itself but serves as an auxiliary component to pause trading during impactful news.

## Usage Tips

1. Attach the strategy to a security and start it inside the AlgoTrader sample or your own runner.
2. Monitor the strategy logs; they will display when the filter becomes active and which event triggered it.
3. Combine the strategy with another trading strategy by checking the `IsNewsActive` flag before calling `BuyMarket`, `SellMarket`, etc.
4. Adjust currency and keyword filters to focus on the macroeconomic releases that matter for your trading instruments.

