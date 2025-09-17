# Daily Range Strategy

## Overview
This strategy is a StockSharp conversion of the MetaTrader 5 expert advisor `MQL/23334/Daily range.mq5`. The original EA tracks the highest and lowest prices reached over the last few days, offsets these levels by a configurable percentage of the daily range, and trades breakouts. The C# port preserves the behaviour while adopting StockSharp's high-level strategy API.

## Strategy logic
### Range calculation
* The strategy stores aggregated statistics for each trading day (high, low, last close).
* A sliding window of `SlidingWindowDays` recent days (including the current one) is maintained.
* `RangeMode` selects how the reference range is computed:
  * **HighestLowest** – the distance between the highest high and the lowest low in the window.
  * **CloseToClose** – the average absolute change between consecutive daily closing prices inside the window.
* Once the configured `StartTime` is reached on a new day, the strategy rebuilds the upper and lower breakout levels:
  * `Upper = Highest + Range × OffsetCoefficient`
  * `Lower = Lowest − Range × OffsetCoefficient`
* Until `StartTime` is reached, the previous day's breakout levels remain active (mirroring the MQL implementation).

### Entry rules
* A long entry is triggered when the closing price of the processed candle is greater than or equal to the current upper level and fewer than `MaxPositionsPerDay` long entries were opened on the same day.
* A short entry is triggered when the closing price falls to or below the lower level and the daily short entry limit has not been reached.
* When flipping from an existing position to the opposite side, the strategy first offsets the outstanding volume and then adds the new `Volume` on top, matching the netting behaviour of the original EA.
* Signals are evaluated only on finished candles delivered by the configured `CandleType` subscription and only when `IsFormedAndOnlineAndAllowTrading()` reports that trading is allowed.

### Exit rules
* Stop-loss and take-profit distances are derived from the current range: `Range × StopLossCoefficient` and `Range × TakeProfitCoefficient` respectively.
* For long positions, a close order is sent if the candle low touches the stop level or the high exceeds the take-profit level.
* For short positions, a close order is sent if the candle high hits the stop level or the low crosses the take-profit level.
* Setting either coefficient to zero disables the corresponding protection.

### Risk controls and limits
* Separate daily counters are maintained for long and short entries. They reset every time a new trading day starts.
* The `Volume` property of the base `Strategy` controls the size of additional entries.
* No pending orders are registered; exits are executed with market orders on the next strategy iteration after the condition is detected.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `RangeMode` | Determines how the daily range is calculated (`HighestLowest` or `CloseToClose`). | `HighestLowest` |
| `SlidingWindowDays` | Number of calendar days included in the sliding window used for the range calculation. | `3` |
| `StopLossCoefficient` | Multiplier applied to the current range to define the stop-loss distance. | `0.03` |
| `TakeProfitCoefficient` | Multiplier applied to the current range to define the take-profit distance. | `0.05` |
| `OffsetCoefficient` | Additional offset applied to the breakout levels above the high and below the low. | `0.01` |
| `MaxPositionsPerDay` | Maximum number of entries allowed per direction during a single trading day. | `3` |
| `StartTime` | Time of day when a fresh range is calculated for the current session. | `10:05` |
| `CandleType` | Candle subscription used for both range calculation and signal evaluation. | `15-minute time frame` |

## Implementation notes
* The strategy relies exclusively on StockSharp's high-level `Strategy` infrastructure (`SubscribeCandles`, `WhenNew`, and market orders) and does not manipulate raw order books.
* Range statistics are stored without using indicator value look-ups; all computations happen inside the strategy, in line with the repository guidelines.
* Protective orders are simulated by monitoring candle extremes rather than registering separate stop/limit orders, which keeps the implementation portable across different adapters.
* Python support is intentionally omitted as requested. Only the C# version is provided in this folder.
* For live trading ensure that sufficient historical candles are available so that the first range calculation has enough data to work with.
