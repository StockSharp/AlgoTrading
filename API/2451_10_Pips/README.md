# Ten Pips Strategy

This hedging strategy opens long and short positions at the same time. Each position uses fixed take profit and stop loss levels measured in price units and can be protected by a trailing stop. When one side closes, the strategy immediately opens a new position in the same direction to keep both sides active.

## Parameters
- `TakeProfitBuy` – take profit distance for long positions.
- `StopLossBuy` – stop loss distance for long positions.
- `TrailingStopBuy` – trailing stop distance for long positions.
- `TakeProfitSell` – take profit distance for short positions.
- `StopLossSell` – stop loss distance for short positions.
- `TrailingStopSell` – trailing stop distance for short positions.
- `Volume` – order size used for all trades.

## Notes
- Positions are opened with market orders.
- Protective orders are registered for each side separately.
- Trailing stops are updated when the market moves in a favourable direction.
