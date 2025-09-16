# RandomT Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "RandomT". The original EA waits for a ZigZag swing that coincides with a confirmed fractal and then filters the entry with a MACD comparison. The StockSharp version keeps the same decision process: it watches a configurable number of candles (`BarWatch`), confirms that a five-bar fractal marks the most recent swing extreme, and only trades when the MACD main line is above or below the signal line on the same historical bar.

## Trading logic
- Build rolling candle buffers and compute the MACD signal on each finished bar of the selected timeframe (`CandleType`).
- Look `Shift` bars into the past and check whether that bar forms an up or down fractal (two candles on each side).
- Validate the fractal against the surrounding price action: the high must be the greatest value, or the low the smallest value, inside the `BarWatch` lookback window. This mirrors the ZigZag swing confirmation used by the MetaTrader version.
- For a short setup the MACD main value must be greater than the signal value on the shifted bar. For a long setup the opposite comparison must be true.
- When a signal appears the strategy uses a single market order whose volume neutralises any opposite position before opening the new trade.

## Trailing stop management
- The trailing block activates only when `UseTrailingProfit` is enabled and the floating profit (converted through `PriceStep` and `StepPrice`) exceeds `MinProfit`.
- The trailing distance is measured in price points. When `AutoStopLevel` is `true`, the engine uses `StartStopLevelPoints`; otherwise it uses `StopLevelPoints`.
- For long positions the stop tracks `ClosePrice - distance`, for short positions it follows `ClosePrice + distance`. If the candle pierces the stop level the strategy closes the trade with a market order.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Base trade size in lots used for every entry. |
| `BarWatch` | Number of bars used to validate that a fractal is also a ZigZag swing extreme. |
| `Shift` | Number of bars back in history that are evaluated for signals. Should stay at 2 for classic fractals. |
| `UseTrailingProfit` | Enables the trailing stop logic. |
| `AutoStopLevel` | Switches the trailing distance to `StartStopLevelPoints`. |
| `StartStopLevelPoints` | Alternate trailing distance (points). |
| `StopLevelPoints` | Primary trailing distance (points). |
| `MinProfit` | Minimum floating profit (account currency) required before trailing is applied. |
| `CandleType` | Timeframe used for candles and indicator calculations. |
| `MacdFastLength` | Fast EMA period for the MACD filter. |
| `MacdSlowLength` | Slow EMA period for the MACD filter. |
| `MacdSignalLength` | Signal EMA period for the MACD filter. |

## Notes
- The strategy calculates fractals internally (two bars on each side) and reuses the result for ZigZag validation, closely matching the buffers accessed in the MQL code.
- The ZigZag confirmation is approximated by checking the surrounding `BarWatch` candles instead of re-running the full MetaTrader indicator, which keeps the behaviour deterministic inside StockSharp.
- Trailing-stop profit is derived from the instrument's `PriceStep` and `StepPrice`. Verify these values for your instrument before running the strategy.
