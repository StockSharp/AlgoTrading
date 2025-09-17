# 4122 Order Guardian

## Overview
The strategy is a high-level StockSharp conversion of the MetaTrader expert advisor `MQL/9210/OrderGuardian.mq4`. It acts as a custodial module for existing positions by continuously recomputing protective take-profit and stop-loss levels. When price breaks one of the limits, the strategy closes the entire position at market, mirroring the original behaviour.

The implementation keeps the parameter names and defaults of the MQL version wherever possible. Instead of scanning manual chart trendlines or channels, StockSharp exposes equivalent manual price inputs that can be updated on the fly. Optional chart guide lines and status messages replicate the on-screen feedback provided by the MetaTrader script.

## Strategy logic
1. **Data processing** – The strategy subscribes to the configured candle type and evaluates logic on completed candles only, preventing intrabar noise.
2. **Level calculation**
   - *Envelope mode*: a moving average is shifted by the requested number of bars and multiplied by `(1 + deviation %)`. The resulting value is used for both long and short targets.
   - *Manual mode*: dedicated parameters provide absolute price levels for long and short take-profit / stop-loss lines.
   - *Parabolic SAR*: the indicator is sampled on every finished candle, shifted by the specified number of bars and reused for both long and short stops.
3. **Position management** – When a long position is open the strategy compares candle highs/lows against the active take-profit and stop-loss. A breakout closes the position using a market order. Short positions use symmetrical checks.
4. **Visual feedback** – When enabled, guide lines connecting the latest two candle closes show the currently active levels. Every time levels change the strategy logs a `S/L @ ...   T/P @ ...` message similar to the original `Comment()` output.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle type processed by the strategy. |
| `TakeProfitMethod` | Level source: `Envelope` (moving average deviation) or `ManualLine` (manual prices). |
| `StopLossMethod` | Level source: `Envelope`, `ManualLine`, or `ParabolicSar`. |
| `TakeProfitPeriod` | Length of the moving average used for the take-profit envelope. |
| `StopLossPeriod` | Length of the moving average used for the stop-loss envelope. |
| `TakeProfitMaMethod` | Moving average algorithm for the take-profit envelope (simple, exponential, smoothed, linear weighted). |
| `StopLossMaMethod` | Moving average algorithm for the stop-loss envelope. |
| `TakeProfitPriceType` | Price source feeding the take-profit moving average. |
| `StopLossPriceType` | Price source feeding the stop-loss moving average. |
| `TakeProfitDeviation` | Percentage added on top of the shifted take-profit moving average. |
| `StopLossDeviation` | Percentage added on top of the shifted stop-loss moving average. |
| `TakeProfitShift` | Number of completed candles used as shift for the take-profit moving average. |
| `StopLossShift` | Number of completed candles used as shift for the stop-loss moving average or SAR value. Automatically forced to at least 1 when `ParabolicSar` is selected. |
| `ManualTakeProfitLong` | Manual take-profit for long positions (0 disables the level). |
| `ManualTakeProfitShort` | Manual take-profit for short positions (0 disables the level). |
| `ManualStopLossLong` | Manual stop-loss for long positions (0 disables the level). |
| `ManualStopLossShort` | Manual stop-loss for short positions (0 disables the level). |
| `SarStep` | Acceleration factor for the Parabolic SAR. |
| `SarMaximum` | Maximum acceleration factor for the Parabolic SAR. |
| `ShowLines` | Enables or disables guide line drawing on the chart. |

## Usage notes
- The strategy does **not** open new positions. Attach it to a portfolio or to other strategies to supervise and close existing trades.
- Manual prices remain live: modify the parameter value while the strategy is running to mimic moving chart objects.
- Envelope mode applies the same value to long and short sides. Use positive deviations for targets above the moving average and negative deviations for levels below it.
- Parabolic SAR mode keeps the original quirk of evaluating the value one bar back (shift >= 1) to avoid using partially formed indicator values.
- The latest formatted status line is exposed through the `StatusLine` property for dashboards or logging.

## Differences versus the MetaTrader version
- Manual trendline / channel detection has been replaced by explicit price inputs because StockSharp does not expose MetaTrader-style chart object enumeration.
- Orders are aggregated at the strategy position level. All open volume is closed when a level is triggered instead of iterating per ticket.
- Horizontal guide lines are drawn with StockSharp chart primitives instead of native MetaTrader objects, but they update after each completed candle just like the original.
