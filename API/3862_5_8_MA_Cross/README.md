# 5/8 EMA Cross Strategy

## Overview
The **5/8 EMA Cross Strategy** replicates the MetaTrader expert advisor `5_8macrossv2.mq4` by comparing two configurable moving averages on the same symbol. A bullish crossover of the fast moving average above the slow one opens long positions, while a bearish crossover opens short positions. The ported version follows StockSharp high-level patterns and adds optional take-profit, stop-loss, and trailing-stop management.

## Trading Logic
- Two moving averages are calculated on the selected candle subscription. By default, a 5-period exponential MA on close prices is compared to an 8-period exponential MA on open prices.
- When the fast MA crosses above the slow MA on the latest finished candle, the strategy opens or reverses into a long position. If a short position is active, its volume is included in the new market buy order to flip direction.
- When the fast MA crosses below the slow MA, the strategy opens or reverses into a short position using the same volume-normalisation logic.
- MA shift parameters emulate the original horizontal offset. Positive values delay the signal by that many closed candles; negative values are rounded to zero because forward-shifted values are unavailable in real-time data.

## Risk Management
- **Take-profit** and **stop-loss** distances are expressed in pips (price steps). When a long position is opened, protective levels are placed above and below the entry price respectively; the logic mirrors for shorts.
- **Trailing stop** (also in pips) constantly tightens the protective level as price moves in favour of the position. For longs the trailing stop only moves upward; for shorts it only moves downward.
- If any protective condition is met on a finished candle (high hits take-profit, low hits stop-loss or trailing level), the strategy exits the position with a market order and resets its internal state.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Order volume for new entries. The strategy adds the absolute position size when reversing. |
| `TakeProfitPips` | `decimal` | `40` | Distance from entry in pips for closing the position with profit. Set to `0` to disable. |
| `StopLossPips` | `decimal` | `0` | Distance from entry in pips for protective stop-loss. Set to `0` to disable. |
| `TrailingStopPips` | `decimal` | `0` | Trailing-stop distance in pips. Set to `0` to disable. |
| `FastPeriod` | `int` | `5` | Period of the fast moving average. |
| `FastShift` | `int` | `-1` | Horizontal shift for the fast MA. Negative values are treated as zero in this port. |
| `FastMethod` | `MovingAverageMethod` | `Exponential` | Smoothing algorithm for the fast MA (Simple, Exponential, Smoothed, LinearWeighted). |
| `FastPrice` | `AppliedPrice` | `Close` | Candle price used for the fast MA. |
| `SlowPeriod` | `int` | `8` | Period of the slow moving average. |
| `SlowShift` | `int` | `0` | Horizontal shift for the slow MA. |
| `SlowMethod` | `MovingAverageMethod` | `Exponential` | Smoothing algorithm for the slow MA. |
| `SlowPrice` | `AppliedPrice` | `Open` | Candle price used for the slow MA. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(30).TimeFrame()` | Candle series used for calculations. |

## Notes
- The conversion keeps the logic focused on finished candles to avoid premature signals.
- Trailing stops and profit targets are computed with `Security.PriceStep`; if a symbol does not define it, the risk parameters remain inactive.
- The Python version is intentionally omitted per task requirements.
