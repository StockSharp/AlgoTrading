# ColorX2MA Digit NN3 MMRec Strategy

## Overview
- Recreates the triple time frame Expert Advisor based on the ColorX2MA Digit indicator.
- Uses a custom double-smoothed moving average indicator that mimics the original X2MA logic with selectable smoothing methods (Simple, Exponential, Smoothed, Linear Weighted, Jurik, Kaufman Adaptive).
- Applies three independent indicator instances (12h, 6h, 3h by default); each instance can independently open or close long/short exposure according to its own settings.
- Aggregates the desired volume of every time frame and trades the difference with market orders so the net position always matches the sum of individual signals.
- Signals are confirmed after `SignalBars` consecutive bars with the same slope direction, which emulates the `SignalBar` shift in the MQL version.
- Includes optional switches to allow or forbid opening/closing of long and short exposure separately for every time frame, reproducing the “Must Trade” flags of the original.

## Parameters
- **A/B/C Candle Type** – data type (time frame) for every indicator instance.
- **Fast/Slow Method** – smoothing method for the first and the second moving average inside the X2MA clone.
- **Fast/Slow Length** – period of the respective moving averages (defaults: 12 and 5).
- **Signal Bars** – number of consecutive bars required before accepting a new direction (default: 1).
- **Digits** – rounding precision applied to the indicator output before slope calculation (simulates the `Digit` input).
- **Price Type** – price source used by the indicator (close, open, median, typical, weighted, simplified, quarter, TrendFollow and DeMark formulas).
- **Allow Long/Short Entry/Exit** – boolean flags that control whether a specific time frame may open or close long/short exposure.
- **Volume** – traded volume contributed by the time frame when it is long (positive) or short (negative).

## Signals and Position Management
1. Each time frame processes finished candles only and updates its indicator value.
2. When the slope of the double-smoothed average turns positive (color index 0 in the MQL indicator) and remains so for the configured number of bars, the context becomes bullish:
   - Existing short exposure is closed if `Allow Short Exit` is enabled.
   - A long position of the configured volume is opened if `Allow Long Entry` is enabled.
3. When the slope turns negative (color index 2), the context becomes bearish:
   - Existing long exposure is closed if `Allow Long Exit` is enabled.
   - A short position of the configured volume is opened if `Allow Short Entry` is enabled.
4. The strategy sums the desired volumes from the three time frames and sends a market order for the difference with the current portfolio so the global `Position` always reflects the combined intent.

## Notes
- Unsupported smoothing types from the MQL library (JurX, Parabolic MA, T3, VIDYA/AMA variations) are not exposed; if required they can be mapped manually.
- The custom indicator rounds values using `Digits` and works only on finished candles, avoiding intrabar repainting.
- No built-in stop-loss or take-profit is added because the original uses MMRec money management; the `Volume` parameters allow manual sizing instead.
