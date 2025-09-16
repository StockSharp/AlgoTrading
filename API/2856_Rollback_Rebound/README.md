# Rollback Rebound Strategy

## Overview
The Rollback Rebound Strategy is a C# conversion of the "TST (barabashkakvn's edition)" MQL5 expert advisor. It monitors a single instrument on the timeframe specified by the `CandleType` parameter and searches for strong moves that retrace back inside the bar range. When a bullish bar fades from its high by more than the rollback threshold the strategy buys, while an equivalent bearish retracement triggers a sell. The implementation uses StockSharp's high-level candle subscription API and manages all protective orders in pip units that are converted into absolute price offsets.

Pip distances are computed from the instrument `PriceStep`. For symbols that quote with three or five decimals the strategy automatically multiplies the step by ten to match the MetaTrader definition of a pip. All position sizing is taken from the base `Volume` property of the strategy.

## Entry Logic
- Process only finished candles from the configured `CandleType` series.
- With `ReverseSignal = false` (default):
  - **Long setup:** the candle closes below its open and the difference between the candle high and close exceeds `RollbackRatePips` (converted to price). This indicates that price expanded upward and then rolled back deep enough to qualify for a contrarian long entry.
  - **Short setup:** the candle closes above its open and the difference between the close and the candle low exceeds `RollbackRatePips`. This mirrors the long logic on the bearish side.
- When `ReverseSignal = true` the roles of the long and short conditions are swapped, allowing the trader to flip the direction without changing the other parameters.
- New entries are only placed when the current position is flat or in the opposite direction. The executed volume equals `Volume + |Position|` so that an opposing position is closed before establishing the new trade.

## Exit Logic
- On entry the strategy stores stop-loss and take-profit levels based on the configured pip offsets. When the candle range touches a level, the position is closed with a market order.
- `StopLossPips = 0` or `TakeProfitPips = 0` disables the corresponding protective level.
- Trailing logic becomes active once the floating profit exceeds `TrailingStopPips + TrailingStepPips` (in price terms).
  - For long trades the stop ratchets to `highest price - TrailingStopPips` whenever the new level is at least `TrailingStepPips` above the previous stop.
  - For short trades the stop ratchets to `lowest price + TrailingStopPips` when the new level is at least `TrailingStepPips` below the previous stop.
  - If the market reverses and crosses the trailing stop, the position is exited immediately.
- When no position is open all internal state variables are cleared to avoid stale data.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for signal calculation. | 15-minute time frame |
| `StopLossPips` | Distance of the protective stop in pips. Set to zero to disable. | 30 |
| `TakeProfitPips` | Distance of the take profit in pips. Set to zero to disable. | 90 |
| `TrailingStopPips` | Trailing stop offset in pips. Set to zero to disable trailing. | 1 |
| `TrailingStepPips` | Extra profit (in pips) required before the trailing stop can move again. Must be positive when trailing is enabled. | 15 |
| `RollbackRatePips` | Minimum pullback from the bar extreme that validates a signal. | 15 |
| `ReverseSignal` | Inverts the entry direction (long signals become short and vice versa). | false |

## Usage Notes
- Set the `Volume` property before starting the strategy; it defines the traded quantity for each order.
- Trailing requires `TrailingStopPips > 0` and `TrailingStepPips > 0`. The strategy throws an error at start if this relationship is violated.
- Because the original expert evaluated ticks inside the active bar, the C# port uses the finished candle high/low/close to approximate the same behaviour. The difference is negligible for most backtests and keeps the implementation aligned with StockSharp's high-level API.
- The strategy works with a single security. To trade multiple instruments, create separate strategy instances.
