# Semilong WWW Forex Instruments Info Strategy

## Overview
This strategy replicates the behaviour of the "Semilong" MetaTrader expert. It monitors the distance between the current bid price and two historical closing prices that are separated by configurable shifts. When the current market trades far enough below (or above) the older close while the older close has also moved away from an even older reference, the strategy opens a long (or short) position. Position management mirrors the original script with configurable take profit, stop loss, optional trailing stop, and an auto-lot module that reduces size after consecutive losses.

## Signal Generation
- **Historical shifts** – `ShiftOne` selects how many finished candles back the first comparison close is taken from, while `ShiftTwo` adds an extra offset for the second close.
- **Deviation filters** – `MoveOnePoints` defines how far the current bid must trade away from the first shifted close, and `MoveTwoPoints` measures the distance between both shifted closes.
- **Long setup** – Triggered when the current bid is at least `MoveOnePoints` below the first shifted close and the first shifted close is at least `MoveTwoPoints` above the second shifted close.
- **Short setup** – Triggered when the current bid is at least `MoveOnePoints` above the first shifted close and the first shifted close is at least `MoveTwoPoints` below the second shifted close.
- The strategy waits for completed candles, ignores signals when orders are already active, and requires positive free margin before trading.

## Trade Management
- **Initial protective orders** – Instead of registering pending orders, the strategy emulates the original behaviour by tracking the entry price and exiting the market once the move reaches:
  - `ProfitPoints` (plus the current spread) in favour of the position.
  - `LossPoints` against the position.
- **Trailing stop** – When `TrailingPoints` is greater than zero, the strategy records the best price reached after entry. If price retraces by the trailing distance, the position is closed.
- **Single position policy** – Only one market position is allowed at a time; new signals are ignored while a trade is running or while close orders are pending.

## Position Sizing
- **Fixed volume** – When `UseAutoLot` is disabled, each trade uses `FixedVolume` (adjusted to the instrument volume step and bounds).
- **Auto-lot calculation** – When enabled, the free margin is divided by `AutoMarginDivider * 1000` and rounded to the nearest whole lot. If at least two losing trades have occurred consecutively, the volume is reduced by `lossStreak / DecreaseFactor` proportionally, mimicking the MT4 decrease logic.
- Volume is clamped between `FixedVolume` and 99 lots and then snapped to the instrument’s volume step/min/max limits.

## Additional Notes
- Spread is read from the current best ask/bid and used to enlarge the profit target, matching the original EA.
- Free margin is approximated from the connected portfolio (`CurrentValue - BlockedValue`), falling back to the current equity if margin data is not available.
- All runtime logging, charting, and optimisation hooks are left to StockSharp’s standard infrastructure so the strategy can be optimised via the designer or run directly in the API project.
