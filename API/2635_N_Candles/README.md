# N Candles Strategy

## Concept
The N Candles strategy scans the market for consecutive candles that all close in the same direction. Once a configurable number of bullish or bearish candles has appeared, the strategy enters in the direction of the sequence. The implementation is a direct conversion of the MetaTrader "N Candles v4" expert advisor and preserves its risk controls, pip-based configuration, and optional trailing stop behaviour within the StockSharp high-level API.

## Entry Conditions
- Every finished candle is evaluated once.
- Candles that close up are counted as bullish, candles that close down are counted as bearish, and doji candles reset the sequence.
- When `ConsecutiveCandles` bullish (or bearish) candles appear in a row, the strategy submits a market order in the direction of the move.
- Hedging-style stacking or netting-style exposure caps are applied depending on the selected `AccountingMode`.

## Exit Management
- `StopLossPips` and `TakeProfitPips` define static exit levels measured in pips from the average entry price of the active position.
- If `TrailingStopPips` is greater than zero, the stop level trails the most favourable price:
  - When no fixed stop exists (for example when `StopLossPips` is zero) the strategy waits until price moves by `TrailingStopPips` in favour of the trade before placing a break-even stop.
  - Once a stop has been set, it moves towards the market when the distance between price and the stop exceeds `TrailingStopPips + TrailingStepPips`.
- Protective levels are recalculated whenever position size changes and are checked against every finished candle, guaranteeing that any stop-loss or take-profit event closes the trade immediately.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `ConsecutiveCandles` | Number of identical candles required to trigger an entry. | 3 |
| `TakeProfitPips` | Take-profit distance in pips. Use zero to disable the target. | 50 |
| `StopLossPips` | Stop-loss distance in pips. Use zero to disable the stop. | 50 |
| `TrailingStopPips` | Trailing stop distance in pips. Zero disables trailing. | 10 |
| `TrailingStepPips` | Additional movement required before the trailing stop advances. | 4 |
| `MaxPositionsPerDirection` | Maximum number of stacked entries per direction when hedging. | 2 |
| `MaxNetVolume` | Maximum absolute net position size when operating in netting mode. | 2 |
| `AccountingMode` | Switch between `Netting` (volume cap) and `Hedging` (entry count cap). | Netting |
| `CandleType` | Candle aggregation used for pattern detection. | 1-minute candles |

All pip-based parameters are converted to price offsets using the instrument tick size. If the security has 3 or 5 decimal places the pip size is scaled by a factor of ten to mirror MetaTrader's definition.

## Implementation Notes
- The strategy relies on the StockSharp high-level candle subscription (`SubscribeCandles`) and avoids manual history buffers.
- Protective logic keeps track of the highest (for longs) or lowest (for shorts) price seen after entry to emulate the original trailing behaviour.
- Position limits adapt automatically to the base strategy `Volume`. Increasing `Volume` expands both stop and take-profit order sizes proportionally.
- Logging messages are emitted whenever a protective exit (stop or take profit) closes a position, providing clarity during backtests.

## Usage Tips
- Choose `Hedging` mode when simulating platforms that allow multiple tickets per direction, or stay with `Netting` to mirror single-position accounts.
- Set `TrailingStepPips` to zero for a classical trailing stop that moves whenever the market advances by `TrailingStopPips`.
- Because exits are evaluated on completed candles, consider a shorter candle interval if intrabar precision is critical.
