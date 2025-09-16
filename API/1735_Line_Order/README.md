# Line Order

Line Order strategy is a translation of the MQL4 script "LineOrder" (10715). The strategy opens a position when the market price reaches a predefined entry line and then manages the position with stop-loss, take-profit, and optional trailing stop.

## Parameters

- `Entry Price` – price level that triggers a position.
- `Stop Loss (pips)` – distance from entry to initial stop loss.
- `Take Profit (pips)` – distance from entry to take profit.
- `Trailing Stop (pips)` – optional trailing stop distance. When set to zero, trailing is disabled.
- `Candle Type` – type of candles used for processing.

## Trading Logic

1. The strategy subscribes to the selected candle series.
2. When a finished candle closes above the entry price, a long position is opened. When it closes below the entry price, a short position is opened.
3. After entering, stop-loss and take-profit levels are calculated using the instrument's price step.
4. If trailing stop is enabled, the stop level moves in the trade's direction.
5. The position is closed when price hits either the stop-loss or take-profit level.

This is a simplified port of the original MQL script, focusing on automated order execution at a user-defined line.
