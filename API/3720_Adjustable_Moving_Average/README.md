# Adjustable Moving Average Strategy

This strategy recreates the MetaTrader "Adjustable Moving Average" expert advisor using StockSharp's high-level API. Two moving averages of the same type but different lengths monitor their distance. When the faster curve crosses the slower one by at least a configurable gap the strategy closes any opposite position and optionally opens a trade in the new direction. Additional session filters, protective exits and an optional trailing stop provide the same operational flexibility as the original robot.

## Trading logic

- Two moving averages (fast and slow) share the same calculation method. The faster period is automatically set to the smaller input, the slower period to the larger input.
- A signal is produced only after both moving averages are fully formed and their absolute distance exceeds the `MinGapPoints` threshold converted into price units.
- When the fast MA is above the slow MA by the required gap the internal signal state becomes bullish. A bearish state is recorded when the slow MA is above the fast MA.
- A state flip closes any existing position if `CloseOutsideSession` is enabled or the current time is within the session window. New orders follow the selected `Mode` (buy only, sell only, or both) and use either a fixed lot or the auto-lot sizing rule.
- Protective logic is checked on every finished candle:
  - Stop loss and take profit distances are measured in instrument points and evaluated against the candle range.
  - The trailing stop activates once price moves in favour of the position by at least `TrailStopPoints` points. The stop is tightened only when the session filter allows trailing or `TrailOutsideSession` is enabled. Once the stop is in place it remains active even outside trading hours.

## Position sizing

- With `EnableAutoLot = false` the strategy sends the `FixedLot` volume (after applying instrument step, minimum and maximum limits).
- With `EnableAutoLot = true` the volume is approximated from the available portfolio value: `(PortfolioValue / 10,000) * LotPer10kFreeMargin`, rounded to one decimal lot. The computed volume is also aligned to the exchange constraints.

## Parameters

| Name | Type / Default | Description |
| --- | --- | --- |
| `CandleType` | `TimeFrame` = 5-minute candles | Timeframe used for moving-average calculations. |
| `FastPeriod` | `int` = 3 | Short moving-average length. Must differ from `SlowPeriod`. |
| `SlowPeriod` | `int` = 9 | Long moving-average length. Must differ from `FastPeriod`. |
| `MaMethod` | `MovingAverageMethod` = Exponential | Moving-average algorithm (Simple, Exponential, Smoothed, Weighted). |
| `MinGapPoints` | `decimal` = 3 | Minimum distance between the fast and slow averages in instrument points. Converted using the instrument price step. |
| `StopLossPoints` | `decimal` = 0 | Protective stop distance in instrument points. Set to zero to disable. |
| `TakeProfitPoints` | `decimal` = 0 | Profit target distance in instrument points. Set to zero to disable. |
| `TrailStopPoints` | `decimal` = 0 | Trailing stop distance in instrument points. Set to zero to disable. |
| `Mode` | `EntryMode` = Both | Allowed direction for new trades (Both, BuyOnly, SellOnly). |
| `SessionStart` | `TimeSpan` = 00:00 | Session start time (platform clock). |
| `SessionEnd` | `TimeSpan` = 23:59 | Session end time (platform clock). Supports overnight sessions when `SessionEnd < SessionStart`. |
| `CloseOutsideSession` | `bool` = true | If true, opposite positions are closed even outside the session window. |
| `TrailOutsideSession` | `bool` = true | If true, the trailing stop keeps updating after the session closes. |
| `FixedLot` | `decimal` = 0.1 | Volume used when automatic sizing is disabled. |
| `EnableAutoLot` | `bool` = false | Enable volume estimation from portfolio value. |
| `LotPer10kFreeMargin` | `decimal` = 1 | Lots allocated per 10,000 units of portfolio value in auto-lot mode. |
| `MaxSlippage` | `int` = 3 | Retained for completeness; StockSharp market orders do not expose a direct slippage parameter. |
| `TradeComment` | `string` = "AdjustableMovingAverageEA" | Text included in log messages when trades are executed. |

## Notes

- The original MetaTrader version applied stop loss, take profit and trailing stops via order modifications. The StockSharp port emulates the behaviour by evaluating candle ranges and sending opposing market orders.
- Portfolio value is used as an approximation of free margin because MetaTrader's `AccountFreeMargin()` is not available in StockSharp.
- When the instrument lacks a valid `PriceStep`, point-based calculations (gap, stops, trailing) remain inactive.
