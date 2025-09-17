# Explosion Strategy

## Overview
Explosion Strategy is a breakout system converted from the MetaTrader 5 expert advisor "Explosion". The algorithm compares the range of the current completed candle with the previous candle and opens a market position in the direction of the candle body whenever the range expansion exceeds a configurable ratio. The StockSharp version keeps the original money-management features and adds convenient parameters for schedule control and trailing stop management.

## Trading Rules
- **Range Expansion:** Calculate the current candle range (`High - Low`) and compare it with the previous candle range. If the current range is greater than the previous range multiplied by `Range Ratio`, a signal is generated.
- **Direction Filter:**
  - If the candle closes above its open and the current position is flat or short, a long market order is sent.
  - If the candle closes below its open and the current position is flat or long, a short market order is sent.
- **Trading Window:** Signals are accepted only when the candle close time falls between `Start Hour` and `End Hour` (inclusive).
- **Daily Limit:** When `One Trade Per Day` is enabled, only the first qualifying entry of the trading day is executed.
- **Pause Between Trades:** After a position entry the strategy waits `Pause (sec)` seconds before accepting a new signal.
- **Maximum Exposure:** The net position size cannot exceed `Max Positions * Order Volume`.

## Exits and Risk Management
- **Initial Protection:** Optional stop-loss and take-profit levels are defined in price steps and calculated from the entry price.
- **Trailing Stop:** When enabled, the stop-loss is moved closer to price after a minimum profit threshold (`Trailing Stop + Trailing Step`) is achieved. The trailing logic maintains the same behaviour as in the original EA.
- **Manual Close on Targets:** If the candle range hits either the stop-loss or take-profit level intrabar, the position is closed using a market order.

## Parameters
- `Candle Type` – Data type used for candle subscription.
- `Order Volume` – Size of each position in lots.
- `Range Ratio` – Multiplier applied to the previous candle range to trigger entries.
- `Max Positions` – Maximum number of lots allowed simultaneously.
- `Pause (sec)` – Minimum time in seconds between entries.
- `Start Hour` / `End Hour` – Trading hours filter (0–23).
- `One Trade Per Day` – Restricts the strategy to one entry per calendar day.
- `Stop Loss` – Initial stop-loss distance in price steps.
- `Take Profit` – Initial take-profit distance in price steps.
- `Trailing Stop` – Trailing stop distance in price steps.
- `Trailing Step` – Additional distance required before trailing is updated.

## Conversion Notes
- The strategy uses the high-level `SubscribeCandles` and `Bind` API for indicator-free signal processing.
- Trailing stop, trading window, pause, and daily limit reproduce the original MQ5 logic.
- Money management is expressed via a single volume parameter; risk-percentage lot sizing from the original script is not supported in this version.
