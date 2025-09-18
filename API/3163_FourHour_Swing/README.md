# Four Hour Swing Strategy

## Overview
The **Four Hour Swing Strategy** ports the MetaTrader "4H swing" expert advisor to the StockSharp high level API. The original system mixes trend following and oscillator confirmations taken from higher time frames. This C# version subscribes to three time frames (entry, confirmation, and macro filter) and recreates the indicator stack with StockSharp components.

## Trading Logic
- The main trend filter uses three exponential moving averages calculated on the typical price of the entry candles. A long setup requires `Fast EMA > Medium EMA > Slow EMA`; a short setup mirrors the condition.
- Stochastic oscillator values are evaluated on the higher confirmation time frame. The %K line must stay above %D for longs and below for shorts.
- Momentum is sampled from the same confirmation candles and converted to the MetaTrader-style ratio around 100. A trade is allowed only if at least one of the last three momentum readings is farther than the configured threshold.
- Monthly (or user defined) MACD values provide the macro direction filter. A buy requires the MACD line above its signal, while a sell checks the opposite relation.

A position is opened on the next base candle once all confirmations are aligned and the account is flat or positioned in the opposite direction (in that case the market order closes and reverses).

## Risk Management
- Fixed stop loss and take profit distances (expressed in pips) are applied immediately after entry.
- An optional trailing stop follows the extreme price reached after the entry.
- Break-even protection can move the stop to the entry price plus an offset once the configured trigger distance is reached.
- An optional MACD exit closes open trades when the macro filter flips.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Default market order volume. | `0.01` |
| `CandleType` | Entry candle type (e.g., 4-hour candles). | `4H` |
| `SignalCandleType` | Confirmation candle type for stochastic and momentum. | `7D` (weekly) |
| `MacdCandleType` | Macro filter candle type. | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | EMA lengths computed on typical price. | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | Stochastic oscillator settings. | `13`, `5`, `5` |
| `MomentumPeriod` | Lookback used by the momentum indicator. | `14` |
| `MomentumThreshold` | Minimum distance from 100 required to validate momentum. | `0.3` |
| `StopLossPips`, `TakeProfitPips` | Protective orders in pips. | `20`, `50` |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable. | `40` |
| `UseBreakEven` | Enables break-even protection. | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Trigger and offset for the break-even move. | `30`, `30` |
| `UseMacdExit` | Close positions when the macro MACD flips. | `false` |

## Notes
- The money-management features (equity stops, currency targets) from the original expert are intentionally omitted to keep the implementation compact.
- The strategy processes only finished candles, matching the MetaTrader bar-by-bar evaluation.
- Default time frames reproduce the common 4-hour setup (weekly confirmation and monthly filter), but every `DataType` parameter can be reconfigured to run on alternative periods.
