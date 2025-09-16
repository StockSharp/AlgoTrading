# N Candles v2
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The strategy searches for a configurable number of consecutive candles that close in the same direction. Once the streak length is reached it opens a market position in the direction of the detected momentum. The implementation mirrors the original MetaTrader 5 "N- candles v2" expert advisor and keeps the logic focused on closed candles to avoid premature signals.

## Strategy Logic
1. Subscribe to the selected candle series and wait for fully closed bars.
2. Categorize each candle as bullish, bearish or neutral (doji). Doji candles reset the streak.
3. Maintain a running counter of consecutive candles with identical direction.
4. When the counter reaches the `CandlesCount` threshold, submit a market order in the same direction. The order size merges the requested `LotSize` with any opposite exposure so the final net position has the intended sign and quantity.
5. Store the entry price and initialise protective levels using the configured stop-loss and take-profit distances.
6. On every new candle update the trailing stop (if enabled) and exit positions whenever the price touches the stop-loss, trailing stop or take-profit levels.

## Position Management
- The initial stop-loss and take-profit are measured in price steps (`Security.PriceStep`). A zero distance disables the corresponding level.
- Trailing stop is optional. When enabled, the stop is tightened by `TrailingStopPips` once price moves favourably by at least `TrailingStepPips` beyond the last stop location.
- Closing a position removes all cached levels so that a fresh streak is required for the next entry.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandlesCount` | Number of consecutive candles that must close in the same direction before trading. | 3 |
| `LotSize` | Position size used for each entry. Opposite exposure is closed automatically. | 1 |
| `TakeProfitPips` | Take-profit distance in price steps from the entry price. | 50 |
| `StopLossPips` | Stop-loss distance in price steps from the entry price. | 50 |
| `TrailingStopPips` | Trailing stop distance in price steps. Set to 0 to disable trailing. | 10 |
| `TrailingStepPips` | Extra distance that price must move before tightening the trailing stop. | 4 |
| `CandleType` | Candle time frame used for signal calculations. | 1 hour candles |

## Notes
- The strategy works with any instrument that provides a valid `PriceStep`. If the instrument reports zero, a value of `1` is used as fallback, matching the behaviour of the source script.
- Signals are generated only on completed candles which keeps behaviour consistent between backtesting and live trading environments.
