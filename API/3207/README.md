# 3207 – MA Trend Strategy

## Overview
The **MA Trend Strategy** replicates the MetaTrader expert *MA Trend.mq5* using StockSharp's high-level API. The bot follows a single linear weighted moving average with a configurable forward shift. When the closing price rises above the shifted average the strategy goes long, while a drop below the average opens short positions. Optional stop-loss, take-profit, and trailing-stop rules mirror the risk controls from the original MQL implementation.

## Trading Logic
1. Subscribe to the configured candle type (defaults to 1-minute time frame) and calculate a moving average using the selected method and price source.
2. Shift the moving average value forward by the requested number of completed candles before comparing it with the most recent close.
3. Generate signals:
   - **Long** – close price above the shifted MA (reversed when `ReverseSignals` is enabled).
   - **Short** – close price below the shifted MA (reversed when `ReverseSignals` is enabled).
4. Apply position management options:
   - Close the opposite exposure before opening a trade when `CloseOpposite` is `true`.
   - Block new entries if `OnlyOnePosition` is enabled and a position already exists.
5. Manage exits with stop-loss, take-profit, and trailing-stop distances expressed in pips. The trailing logic requires price to move by `TrailingStopPips + TrailingStepPips` before tightening the stop, just like the MQL expert.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Order size in lots/contracts. |
| `StopLossPips` | `int` | `50` | Stop-loss distance in pips. Zero disables the fixed stop. |
| `TakeProfitPips` | `int` | `140` | Take-profit distance in pips. Zero disables the target. |
| `TrailingStopPips` | `int` | `15` | Trailing stop distance. Set to zero to disable trailing. |
| `TrailingStepPips` | `int` | `5` | Extra pips required before moving the trailing stop. Must stay positive when `TrailingStopPips` is greater than zero. |
| `MaPeriod` | `int` | `12` | Moving average length. |
| `MaShift` | `int` | `3` | Number of completed bars used to shift the moving average forward. |
| `MaMethod` | `MovingAverageKind` | `Weighted` | Moving average calculation mode (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | Candle price used as indicator input (Close, Open, High, Low, Median, Typical, Weighted). |
| `OnlyOnePosition` | `bool` | `false` | Restrict the strategy to a single open position. |
| `ReverseSignals` | `bool` | `false` | Swap long/short signal directions. |
| `CloseOpposite` | `bool` | `false` | Close opposite exposure before entering a new position. |
| `CandleType` | `DataType` | `1 minute` | Candle type/time frame supplied to the indicator. |

## Notes
- The pip size automatically adapts to instruments with 3/5 decimal pricing to match the original MetaTrader behaviour.
- Trailing-stop validation reproduces the MQL check: if `TrailingStopPips > 0` and `TrailingStepPips <= 0`, the strategy throws an exception during start.
- All indicator updates and order decisions use finished candles only, ensuring deterministic backtests.
