# Color Bears Strategy

The strategy builds a double-smoothed Bears Power oscillator and trades on changes in its slope.

## Idea
1. Calculate an exponential moving average (MA1) of closing prices.
2. Compute Bears Power as the difference between the candle low and MA1.
3. Smooth Bears Power with another exponential moving average (MA2).
4. Track whether the smoothed value rises or falls and react to slope reversals.

## Trading Rules
- When the indicator switches from rising to falling (color 0 → 2), close short positions and open a long one.
- When the indicator switches from falling to rising (color 2 → 0), close long positions and open a short one.
- Each position uses the strategy `Volume` property as order size.

## Parameters
| Name | Description |
|------|-------------|
| `Ma1Period` | Period of the first EMA used to build Bears Power. |
| `Ma2Period` | Period of the smoothing EMA. |
| `CandleType` | Candle timeframe for calculations. |

## Notes
This C# implementation is adapted from the MQL "ColorBears" expert (folder `MQL/14314`).
The algorithm relies on standard StockSharp indicators and high-level API bindings.
