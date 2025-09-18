# Tipu MACD EA Strategy

## Overview
The strategy is a high-level StockSharp port of the **Tipu MACD EA** from MQL4. It trades a single symbol using MACD-based signals and mirrors the original expert advisor features:

* Optional trading-hour filter with two configurable time windows.
* MACD zero-line and signal-line crossover entries with adjustable EMA lengths and shift.
* Automatic position management including take-profit, stop-loss, trailing stop, and breakeven.
* Volume capping that emulates the "maximum lots" setting from the source code.

All operations use market orders. Protective levels are tracked internally and orders are closed once a candle pierces the stop-loss or take-profit levels.

## Trading logic
1. Subscribe to the configured candle type and calculate a `MovingAverageConvergenceDivergenceSignal` indicator (MACD line + signal line).
2. Evaluate MACD values using the selected shift (`MacdShift` 0 = current candle, 1 = previous candle) and build crossover signals:
   * **Zero-line crossover** (optional) – buy when MACD crosses above zero, sell when it crosses below.
   * **Signal-line crossover** (optional) – buy when MACD crosses above the signal line, sell when it crosses below.
3. Before opening a position, ensure the current hour belongs to at least one of the two time windows when the filter is enabled.
4. When a long signal appears:
   * If hedging is disabled and a short is open, optionally close it (`CloseOnReverseSignal`) or skip the new trade.
   * Place a buy market order for the lesser of `TradeVolume` and the remaining volume until `MaxPositionVolume` is reached.
   * Update the long entry snapshot and compute protective stop/take levels if enabled.
5. When a short signal appears follow the symmetric logic for sell orders.
6. While a position is active:
   * Monitor stops and targets on each finished candle and close the trade if either level is breached.
   * When trailing is enabled and price advances by `TrailingPips + TrailingCushionPips`, move the stop to maintain `TrailingPips` distance from price.
   * When the breakeven module is active and profit exceeds `RiskFreePips`, move the stop to the entry price.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle series used for MACD calculations. |
| `TradeVolume` | Volume of each market entry (lots). |
| `MaxPositionVolume` | Maximum cumulative long or short exposure allowed. |
| `UseTimeFilter` | Enables the dual-window trading hour filter. |
| `Zone1StartHour`, `Zone1EndHour` | Start/end hours for the first trading window (inclusive, exchange time). |
| `Zone2StartHour`, `Zone2EndHour` | Start/end hours for the second trading window. |
| `FastPeriod`, `SlowPeriod`, `SignalPeriod` | MACD fast EMA, slow EMA, and signal SMA lengths. |
| `MacdShift` | 0 = evaluate the current bar, 1 = evaluate the previous bar (matching the MQL `iShift`). |
| `UseZeroCross` | Enables MACD zero-line cross entries. |
| `UseSignalCross` | Enables MACD vs. signal-line cross entries. |
| `AllowHedging` | Allows building both long and short exposure without closing the opposite side first. |
| `CloseOnReverseSignal` | Closes the opposite position when a new signal appears (used when hedging is disabled). |
| `UseTakeProfit`, `TakeProfitPips` | Enables and configures the take-profit distance (pips). |
| `UseStopLoss`, `StopLossPips` | Enables and configures the stop-loss distance (pips). |
| `UseTrailingStop`, `TrailingPips`, `TrailingCushionPips` | Enables trailing management, sets trailing distance and cushion (pips). |
| `UseRiskFree`, `RiskFreePips` | Moves the stop to breakeven once profit exceeds the specified pips. |

## Usage notes
* Configure the candle type to match the timeframe used in MetaTrader (default 15-minute bars).
* The pip size is derived from `Security.PriceStep`. If the instrument lacks this metadata, a default of 0.0001 is used.
* The strategy assumes immediate execution of market orders. When running live, ensure proper slippage handling if necessary.
* When both zero-line and signal-line entries are disabled the strategy remains idle.
