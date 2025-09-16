# Breakeven Trailing Stop Strategy

## Overview
- Tick-based trailing stop manager converted from the MetaTrader expert advisor `e_Breakeven_v4`.
- Monitors every trade tick to move a virtual stop-loss once price travels far enough from the entry.
- Closes long or short positions at market when the trailing level is hit, replicating the breakeven-plus-step behaviour of the original EA.
- Includes an optional demo mode that randomly opens positions during testing to demonstrate the trailing logic without an external signal source.

## How It Works
1. The strategy subscribes to trade ticks (`DataType.Ticks`) to emulate the `OnTick` callback used in MQL5.
2. When a position exists and the trailing stop (in pips) plus the trailing step have been exceeded, the stop level is shifted closer to price.
3. For long positions, the stop is placed at `current price - trailing stop` if the move from the entry exceeds `trailing stop + trailing step`.
4. For short positions, the stop is placed at `current price + trailing stop` when the price moves downward by the same distance.
5. If the live price touches or crosses the stored stop level, the strategy exits the entire position at market and resets the trailing state.
6. An internal pip conversion multiplies the broker price step by 10 when the instrument has 3 or 5 decimal digits, matching the MQL5 point-to-pip adjustment.
7. When demo mode is enabled, the strategy opens a random long or short trade (using the configured `Volume`) the first time a new tick arrives after the previous entry was closed.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `TrailingStopPips` | Distance in pips between the current price and the trailing stop. | 10 | Set to `0` to disable trailing completely. |
| `TrailingStepPips` | Additional pip distance required before the stop is advanced again. | 1 | Must be greater than zero when the trailing stop is active, reproducing the EA validation rule. |
| `EnableDemoEntries` | Enables random entries for backtests without an external signal. | `false` | When `true`, the strategy flips a coin on each tick while flat to decide the direction. |

## Position Management Rules
- The strategy does not open positions by itself unless `EnableDemoEntries` is set to `true`.
- Trailing is symmetric for long and short positions and works with any volume size.
- Stop levels are managed internally (virtual) and enforced with market exits, avoiding explicit stop orders that may not be supported by every connector.
- Any manual trade or external strategy can supply the entries; this component will only manage the trailing stop.

## Usage Notes
- Works best with instruments that provide trade ticks so the trailing reacts immediately.
- Ensure `Volume` is configured to the lot size that matches the incoming positions if demo mode is used.
- The pip conversion assumes FX-style pricing where symbols with 3 or 5 decimal places need a ×10 multiplier to turn points into pips.
- The exit is triggered on the first tick that crosses the stored stop price, matching the immediate modification-and-close flow from the MQL logic.

## Differences from the Original MQL5 Expert
- Uses virtual stops with market exits instead of modifying broker-side stop-loss orders because StockSharp strategies typically manage exits through strategy logic.
- Replaces the MetaTrader tester random entry block with the configurable `EnableDemoEntries` flag.
- Converts the point-to-pip logic using `Security.PriceStep` and decimal counting instead of `Symbol().Digits()`.
- All comments and logging are now in English in accordance with repository guidelines.
