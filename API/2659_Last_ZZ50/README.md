# Last ZZ50 Strategy

## Overview
The Last ZZ50 strategy reproduces Vladimir Karputov's "Last ZZ50" expert advisor for MetaTrader.
It uses the ZigZag indicator to track the three most recent turning points and places pending orders at the midpoint of the last two ZigZag legs.
The approach attempts to join breakouts away from the latest swing while cancelling or repositioning orders whenever the ZigZag structure changes.

## Trading Logic
- **Pivot detection** – A ZigZag indicator (depth 12, deviation 5, backstep 3 by default) supplies the latest pivots labelled A (most recent), B and C.
- **BC leg order** – When pivot C differs from B and the new pivot A does not invalidate the leg direction, the strategy places a pending order at `(B + C) / 2`.
  - If the BC leg is rising the order is long, otherwise it is short.
  - Limit versus stop type is selected according to the current price relative to the midpoint.
- **AB leg order** – The same midpoint logic is applied to the AB leg, again using limit or stop orders depending on current price.
- **Session filter** – Trading is limited to a configurable weekday and intraday window (default Monday 09:01 to Friday 21:01). Outside of the window the strategy cancels pending orders and can optionally flatten any position.
- **Trailing exit** – Once a position gains more than the sum of the trailing stop and trailing step thresholds, a protective stop order is trailed behind price to lock in profits.

## Risk Management
- The volume of pending orders equals the multiplier parameter times the instrument's minimum tradable volume.
- Both AB and BC orders are cancelled and re-created whenever the ZigZag pivots change, preventing stale orders from being left in the book.
- Trailing stops only activate after the position is comfortably in profit, reducing premature exits in choppy conditions.

## Parameters
- `LotMultiplier` – Multiplier applied to the minimal tradable volume when sending orders.
- `ZigZagDepth`, `ZigZagDeviation`, `ZigZagBackstep` – Configuration values for the ZigZag indicator.
- `TrailingStopPips`, `TrailingStepPips` – Distance and activation threshold for the trailing stop measured in pips.
- `StartDay`, `EndDay`, `StartTime`, `EndTime` – Trading session boundaries.
- `CloseOutsideSession` – Whether to flatten positions when the time filter is inactive.
- `CandleType` – Candle series used for ZigZag calculations (default 1 hour).

## Indicators
- **ZigZag** – Supplies pivot points that drive order placement and structure validation.
