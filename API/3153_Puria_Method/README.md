# Puria Method Strategy

## Overview
The Puria Method strategy is a trend-following system originally designed for MetaTrader. It combines three moving averages with a MACD trend filter to detect momentum breakouts. The StockSharp conversion keeps the original entry logic and adds modern risk controls such as partial profit taking and automated trailing stops.

## Trading Logic
- Calculate three moving averages using configurable smoothing methods and price sources.
- Evaluate the difference between the slower baseline MA and the two faster MAs on the previous bar. A bullish signal requires both fast MAs to be at least 0.5 points above the baseline; a bearish signal requires the baseline to lead by the same margin.
- Confirm market direction with the MACD main line. Long trades require the previous MACD value to be positive and the recent MACD history to be non-decreasing for the configured number of bars. Short trades require the opposite conditions.
- When an entry is triggered the strategy closes an opposite position (if any) and opens a new net position in the signal direction.

## Risk Management
- **Stop Loss / Take Profit:** Prices are calculated from the entry using pip distances and normalized to the security price step.
- **Trailing Stop:** Once the position moves beyond the trailing threshold plus step, the stop is advanced with every additional trailing step.
- **Partial Exit:** After the price travels a minimum profit distance a configurable fraction of the position is closed to lock in gains.
- **Position Management:** The algorithm keeps track of the highest (long) or lowest (short) price after entry to trigger stop or profit rules when candles pierce those levels.

## Parameters
| Name | Description |
| ---- | ----------- |
| `StopLossPips` | Stop loss distance in pips. |
| `TakeProfitPips` | Take profit distance in pips. |
| `TrailingStopPips` | Trailing stop distance in pips. |
| `TrailingStepPips` | Minimum profit advance before the trailing stop is updated. |
| `MinProfitStepPips` | Minimum distance in pips before taking a partial profit. |
| `MinProfitFraction` | Fraction of the position to close when the minimum profit step is reached. |
| `CandleType` | Primary candle series used by the strategy. |
| `Ma0Period`, `Ma1Period`, `Ma2Period` | Periods for the three moving averages. |
| `Ma0Shift`, `Ma1Shift`, `Ma2Shift` | Optional bar shifts applied to each moving average. |
| `Ma0Method`, `Ma1Method`, `Ma2Method` | Moving average smoothing methods (simple, exponential, smoothed, linear weighted). |
| `Ma0Price`, `Ma1Price`, `Ma2Price` | Candle price sources for the moving averages. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| `MacdTrendBars` | Number of bars used to verify the MACD monotonic trend (minimum 3). |
| `MacdPrice` | Candle price source for the MACD calculation. |

## Notes
- The strategy uses the previous completed bar for MA and MACD comparisons to avoid relying on unfinished candle data.
- Pip size is automatically derived from the security price step and decimal precision.
- Trailing and partial exit features require non-zero configuration values; otherwise the corresponding blocks remain inactive.
- The converted version relies solely on finished candles (`CandleStates.Finished`) and should be paired with a candle series that matches the original chart timeframe.
