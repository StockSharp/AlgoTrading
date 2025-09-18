# Tuyul Uncensored Strategy

## Overview
Tuyul Uncensored is a swing-following strategy that rebuilds the original MetaTrader 5 expert advisor with StockSharp's high-level API. The system observes ZigZag swings, aligns entries with a moving average trend filter, and places limit orders at the 57% Fibonacci retracement of the latest leg. When price revisits that level, the strategy attempts to join the dominant swing while protecting the trade with stop-loss and take-profit levels derived from the same leg.

## Trading Logic
1. **Data preparation**
   - One candle subscription defined by the selected `Candle Type` parameter.
   - A ZigZag indicator (Depth/Deviation/Backstep) is used to track the latest confirmed swing high and swing low.
   - Fast and slow EMAs (default 9/21) provide the directional filter.
2. **Signal detection**
   - When the ZigZag confirms a new pivot (either a new high or a new low), the strategy updates the most recent swing pair.
   - If no orders are active and there is no open position, the previous EMA values determine the trend:
     - Fast EMA above slow EMA → bullish context.
     - Fast EMA below slow EMA → bearish context.
3. **Order placement**
   - In a bullish context the strategy places a **buy limit** order at the 57% retracement between the last swing low and swing high.
   - In a bearish context it places a **sell limit** order at the symmetric 57% retracement from swing high to swing low.
   - Stop-loss is anchored at the opposite ZigZag extreme, while take-profit equals the stop distance multiplied by `Take Profit Multiplier` (default 1.2).
   - Orders remain active for `Wait Bars After Signal` candles; afterwards they are cancelled to wait for a fresh signal.
4. **Position management**
   - Once an order fills the strategy watches subsequent candles. A long position is closed when price reaches either the predefined stop-loss or take-profit. The same mirrored logic applies to short positions.
   - Trading can be limited to specific weekdays. Outside the permitted days all pending orders are removed, but existing positions are left untouched, following the original advisor behavior.

## Parameters
| Name | Description |
|------|-------------|
| `Volume Per Trade` | Order volume submitted with every entry. |
| `TP Multiplier` | Multiplier applied to the stop distance to compute the take-profit offset. |
| `ZigZag Depth` | Number of candles examined when confirming a swing. |
| `ZigZag Deviation` | Minimum deviation (in points) required before the ZigZag validates a new pivot. |
| `ZigZag Backstep` | Minimum number of candles between opposite ZigZag pivots. |
| `Wait Bars After Signal` | Maximum candles to keep the pending order alive before cancellation. |
| `Fast EMA` | Period of the fast exponential moving average used as trend filter. |
| `Slow EMA` | Period of the slow exponential moving average used as trend filter. |
| `Allow Monday … Allow Friday` | Toggles that enable or disable trading on individual weekdays. |
| `Candle Type` | Candle series used for all indicator calculations and trading decisions. |

## Notes
- The Fibonacci retracement ratio is fixed at 57% as in the source EA.
- Stop-loss and take-profit levels are monitored on candle closes; intrabar spikes beyond the thresholds trigger market exits on the next evaluation.
- The strategy keeps a single pending order at a time, mirroring the original implementation.
