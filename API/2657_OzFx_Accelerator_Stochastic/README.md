# OzFx Accelerator Stochastic Strategy

## Overview
- Conversion of the MetaTrader expert advisor *OzFx (barabashkakvn's edition)* to the StockSharp high-level strategy API.
- Combines the Acceleration/Deceleration oscillator (AC) with a stochastic threshold to layer into trends.
- Designed for discretionary-style forex trading where orders are sized in lots and protection is expressed in pips.

## Trading Logic
1. Compute the Acceleration/Deceleration oscillator as the difference between the Awesome Oscillator and its 5-period SMA.
2. Subscribe to a stochastic oscillator with configurable `%K`, `%D`, and slowing periods.
3. When a new candle closes, evaluate the two most recent AC values together with the stochastic level:
   - **Long setup**: `%K` crosses above the configured level, current AC is positive and rising while the previous value was negative.
   - **Short setup**: `%K` crosses below the level, current AC is negative and falling while the previous value was positive.
4. On a valid signal open up to five equal-sized market orders. The first layer mirrors the original EA by launching without a stop/target, while the remaining layers inherit the configured stop loss and staggered take profits.
5. Exit management emulates the original "modok" flag behaviour:
   - When trailing stops are disabled the strategy only tightens stops to breakeven after a profitable exit, and it will close all layers if the stochastic/AC combination flips against the position.
   - With trailing stops enabled the stop follows price once the move exceeds *TrailingStop + TrailingStep*, and the same momentum reversal closes the stack.

## Position Scaling and Targets
- Long positions place four additional layers with take profits at `entry + TakeProfit * i` for `i = 1..4`. Shorts mirror this below price.
- Stop losses (when configured) are attached to every layer except the very first one, exactly like the MT5 script.
- Partial take profits update the internal flag so that the next campaign immediately starts in "modok = true" state, unlocking breakeven protection for the initial layer.

## Risk Management
- `StopLossPips` and `TakeProfitPips` are defined in pips. The strategy converts them using the instrument tick size and digit precision (`5` or `3` decimal pairs count as fractional pips).
- `TrailingStopPips = 0` disables trailing logic and enables breakeven tightening only after a take profit. Any positive value activates the trailing block described above.
- All exits are executed with market orders when the candle range crosses the stored stop or target levels, matching the behaviour of the original expert that relied on broker-side protective orders.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Lot size per layer. | `0.1` |
| `StopLossPips` | Distance for protective stop orders (pips). | `100` |
| `TakeProfitPips` | Base distance between layered take profits (pips). | `50` |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). | `50` |
| `TrailingStepPips` | Additional distance before advancing the trailing stop. | `5` |
| `KPeriod` | Stochastic `%K` lookback. | `5` |
| `DPeriod` | Stochastic `%D` smoothing. | `3` |
| `SmoothingPeriod` | Final smoothing applied to `%K`. | `3` |
| `StochasticLevel` | Threshold separating bullish/bearish regimes. | `50` |
| `CandleType` | Source candle series for calculations. | `4h time-frame` |

## Implementation Notes
- Signals, trailing updates, and protective exits are processed on completed candles to stay consistent with the EA that triggers on new bars.
- The AC indicator is reproduced by binding the Awesome Oscillator and subtracting its 5-period SMA; no low-level indicator buffers are accessed.
- Pip conversion automatically adapts to 4/5-digit forex symbols and falls back to a reasonable default when tick size metadata is missing.
- The strategy keeps an internal ledger of layered entries so that partial take profits and stop adjustments match the per-position logic of the MetaTrader version.
- Because StockSharp executes exits via market orders, trades are flattened when the candle's high/low pierces the stored stop or target levels rather than waiting for broker-side triggers.
