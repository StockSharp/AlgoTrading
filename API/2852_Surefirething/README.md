# Surefirething Strategy

## Overview
The Surefirething strategy recreates the classic MetaTrader 5 expert advisor that places symmetric buy and sell limit orders around the most recent candle close. The system constantly rebuilds the grid after every completed candle, manages protective stops in pip units, and forces a complete flat position ten minutes before midnight server time.

## Candle processing
- Works with a configurable candle type (default: 1-hour time frame).
- After each finished candle the strategy calculates an amplified range: `range = (high - low) * 1.1`.
- It derives two breakout levels from that range:
  - `L4 = close - range / 2` for the buy limit order.
  - `H4 = close + range / 2` for the sell limit order.
- Existing pending orders are cancelled before publishing the new grid so only one buy and one sell limit order remain active.

## Order management
- Buy limit at `L4` and sell limit at `H4` are registered with the configured order volume.
- Once a position opens the opposite pending order is cancelled immediately.
- Every day at **23:50** (server time) the strategy:
  - Cancels any remaining pending orders.
  - Closes the open position at market, if any.
  - Resets all stop/take-profit trackers to start the next session clean.

## Risk management
- Stop-loss and take-profit distances are defined in pips and translated into prices using the instrument price step (5-digit and 3-digit symbols are adjusted to classic pip units automatically).
- A trailing stop (also in pips) can be enabled. Each time price moves beyond `TrailingStopPips + TrailingStepPips`, the stop is advanced to `current price - TrailingStopPips` for longs or `current price + TrailingStopPips` for shorts.
- Both protective levels are monitored on every candle. If the candle trades through the stop or the target, the strategy exits the position using market orders.

## Parameters
- `OrderVolume` – base volume for both limit orders (default: `0.1`).
- `StopLossPips` – stop-loss distance in pips (default: `50`).
- `TakeProfitPips` – take-profit distance in pips (default: `50`).
- `TrailingStopPips` – trailing stop distance in pips (default: `25`).
- `TrailingStepPips` – additional movement in pips required before the trailing stop moves (default: `1`). Must be greater than zero when a trailing stop is enabled.
- `CandleType` – candle data type used for calculations (default: 1-hour time frame).

## Notes
- The implementation matches the original MQL logic by enforcing that the trailing step is non-zero whenever trailing is active.
- No Python implementation is provided for this strategy.
