# AutoTrading Scheduler Strategy

## Overview

The AutoTrading Scheduler strategy replicates the EarnForex MetaTrader expert advisor that toggles MetaTrader's "AutoTrading" switch. The StockSharp port keeps the account flat outside of user-defined time windows and resumes trading when the clock moves back inside an allowed interval. All configuration is performed through readable strings, one for each day of the week.

The module is intentionally signal-agnostic: it does not open new trades on its own. Instead, it supervises the trading state of the host strategy. When the scheduler disables auto trading it cancels all active orders, optionally flattens the current position, and logs the event through `AddInfoLog` so the host application can react.

## Original Logic

* Loads a persistent timetable with multiple time spans per weekday.
* Supports local or broker/server time bases.
* Checks the schedule every second via an internal timer.
* When the clock is outside every span of the current weekday it disables auto trading and can optionally close every open trade and pending order.
* Re-enables auto trading once the clock enters any allowed span again.

## Implementation Notes

* The StockSharp version stores the parsed schedule in memory and recomputes it every time the user edits one of the text parameters.
* Time spans accept multiple formats: `9-12`, `09:30-16:00`, `21.15-23.45`. Minutes are optional and default to `00` when omitted. Separate multiple spans with commas.
* A range whose end equals `00:00` remains active until midnight (e.g. `22-0` means 22:00:00 until 23:59:59). Using `0-0` keeps trading enabled for the whole day.
* Time spans whose end is less than the start automatically wrap to the following day, mirroring the helper logic from the original expert advisor.
* The timer runs every five seconds to balance responsiveness and resource usage.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `SchedulerEnabled` | `bool` | `false` | Master switch that activates the timetable. When disabled the strategy never interferes with trading. |
| `ReferenceClock` | `TimeReference` | `Local` | Chooses between the local machine clock and the exchange/server time supplied by the connector. |
| `ClosePositionsBeforeDisable` | `bool` | `true` | When the scheduler disables auto trading it first cancels every active order and flattens the current position. |
| `MondaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Monday. |
| `TuesdaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Tuesday. |
| `WednesdaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Wednesday. |
| `ThursdaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Thursday. |
| `FridaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Friday. |
| `SaturdaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Saturday. |
| `SundaySchedule` | `string` | `""` | Comma-separated list of trading intervals for Sunday. |

All schedule parameters accept the same syntax. Example: `"09-12, 13:30-17:45, 22-0"`.

## Usage

1. Attach the strategy to the desired security or portfolio.
2. Enter one or more time ranges for the days you want to trade. Leave a day empty to prohibit trading for the entire day.
3. Enable the scheduler by setting `SchedulerEnabled = true`.
4. Decide whether positions should be flattened automatically using `ClosePositionsBeforeDisable`.
5. Monitor the log output: each toggle writes a message with the reason (window opened or closed).

When the current time is inside an allowed range the strategy sets `IsAutoTradingEnabled = true`. Outside every range the property turns `false`, the module cancels working orders, flattens the position if configured, and logs the action.

## Known Limitations

* The strategy only supervises the single security attached to it. Multi-symbol portfolios require multiple scheduler instances or a custom coordinator.
* The timer interval can be adjusted inside the source code (`TimeSpan.FromSeconds(5)`) if a different granularity is required.
* The strategy does not persist the schedule to disk. Use the host application's parameter storage mechanisms if persistence is necessary.
