# Risk Manager

A risk management helper strategy that monitors account equity and closes positions when specified limits are exceeded. It does not open new trades; instead it protects existing positions by enforcing daily loss, per-trade loss and trailing profit rules.

## Overview

The strategy stores the account value at startup and checks risk conditions on every timer tick. If the portfolio drops below the allowed percentage or if an open position loses too much, the strategy closes positions. When a trailing profit is set it also protects accumulated gains.

## How It Works

- **Daily loss** – compares current equity with starting equity and stops trading if the drop exceeds `DailyRisk` percent.
- **Trade loss** – monitors the profit of the current position. If the loss exceeds `TradeRisk` percent of starting equity, the position is closed.
- **Trailing profit** – tracks the highest profit percentage reached during the day and closes all positions if profit falls by `TrailingRisk` percent from that peak.

## Parameters

- `DailyRisk` – maximum allowed daily loss in percent. Default: `5`.
- `TradeRisk` – allowed loss per open position in percent of starting equity. Default: `0` (disabled).
- `TrailingRisk` – profit drop from the daily peak that triggers exit. Default: `0` (disabled).

## Example

Typical configuration:

- `DailyRisk` = `5`
- `TradeRisk` = `2`
- `TrailingRisk` = `3`

## Notes

- Works with a single security bound to the strategy.
- Requires portfolio information to evaluate equity values.
- Designed for protecting manually or externally opened positions.

## Risk

- Risk Level: Low
