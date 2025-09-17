# Day Opening MACD Histogram Strategy

## Overview
This strategy replicates the MetaTrader expert "2 1000 1 0.7% 0.5 500lev st" by entering a trade at the beginning of each new trading day and filtering the direction with the MACD histogram slope. The system was designed for hourly candles and relies on fixed money management parameters converted from the original MQL settings.

## Trading Logic
- The strategy monitors hourly candles and detects the first candle of every new day.
- It evaluates the MACD histogram on the two most recent completed candles of the previous day.
- If the histogram declined between those two bars, the system opens a long position at the first candle of the new day.
- If the histogram increased, it opens a short position instead.
- Only one position can be active at a time. Opposite signals close the current trade before opening the new direction.

## Risk Management
- Initial stop-loss distance: 875 points (converted to price by multiplying with the instrument price step).
- Take-profit distance: 510 points.
- Trailing stop distance: 2172 points. The stop follows the highest (long) or lowest (short) price reached since the entry and overrides the initial stop when it becomes tighter.
- The original break-even option was disabled and therefore omitted here.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Candle series used by the strategy (hourly by default). | 1 hour candles |
| `MacdFastPeriod` | Fast EMA period for the MACD. | 58 |
| `MacdSlowPeriod` | Slow EMA period for the MACD. | 195 |
| `MacdSignalPeriod` | Signal line period for the MACD. | 183 |
| `StopLossPoints` | Stop-loss distance expressed in instrument points. | 875 |
| `TakeProfitPoints` | Take-profit distance in points. | 510 |
| `TrailingStopPoints` | Trailing stop distance in points. | 2172 |

## Notes
- The strategy uses only completed candles to avoid intrabar look-ahead, mirroring the "Use previous bar value" option from the source expert.
- Trailing and fixed exits are handled internally, so additional portfolio protections should remain disabled to prevent double handling of stops.
- The logic assumes the broker uses standard point definitions (price step). Adjust the parameters if the instrument uses a different tick size.
