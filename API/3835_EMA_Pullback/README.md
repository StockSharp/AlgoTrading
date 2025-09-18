# EMA Pullback Strategy

## Overview
The EMA Pullback strategy is a high-level port of the MetaTrader "Ema" expert advisor. It observes a pair of exponential moving averages (EMA) with periods 5 and 10 calculated on median candle prices. When a bullish or bearish crossover appears, the strategy waits for price to retrace towards the previous candle's extreme before entering in the direction of the crossover. Fixed take-profit and stop-loss levels measured in price points manage risk once the position is open.

## Trading Logic
1. Subscribe to the configured candle series (default: 5-minute time frame) and calculate two EMAs on the median price `(high + low) / 2`.
2. Detect a bullish crossover when the fast EMA crosses above the slow EMA, or a bearish crossover when the fast EMA crosses below the slow EMA.
3. Arm a pullback entry after the crossover occurs:
   - For a long setup, wait until the close price retreats to the previous candle high minus the `MoveBackPoints` offset while the fast EMA remains above the slow EMA by at least two price points.
   - For a short setup, wait until the close price returns to the previous candle low plus the `MoveBackPoints` offset while the slow EMA stays above the fast EMA by at least two price points.
4. When the pullback condition is satisfied, send a market order with the configured trade volume.
5. Upon entry, compute static take-profit and stop-loss levels using the `TakeProfitPoints` and `StopLossPoints` settings, converted into absolute price offsets from the entry price.
6. Monitor every finished candle and close the position once either the take-profit or stop-loss level is touched by the candle's high/low.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `TradeVolume` | `0.1` | Volume used for each market order. |
| `FastLength` | `5` | Period of the fast EMA applied to median prices. |
| `SlowLength` | `10` | Period of the slow EMA applied to median prices. |
| `MoveBackPoints` | `3` | Pullback distance, in price points, measured from the previous candle's extreme. |
| `TakeProfitPoints` | `5` | Take-profit distance, in price points. |
| `StopLossPoints` | `20` | Stop-loss distance, in price points. |
| `CandleType` | `5m` | Time frame used for candle subscription and indicator calculations. |

## Notes
- Only fully formed candles are processed to avoid premature signals.
- The strategy automatically aligns the `Strategy.Volume` property with the `TradeVolume` parameter on start.
- All calculations rely on the instrument `PriceStep` to convert point-based distances into absolute prices.
- The strategy opens at most one position at a time and requires a new EMA crossover before preparing another trade.
