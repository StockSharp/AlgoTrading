# Pending Orders By Time Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the classic “Pending orders by time” MetaTrader expert for StockSharp. It runs on a discrete schedule: every day it places symmetric stop orders around the market when a new session hour begins, and it clears all orders plus open positions at a specified closing hour. The implementation keeps the original pip-based inputs, converts them to native price units, and uses the high-level API to manage risk.

## How it works

1. **Time-based trigger** – When a candle that ends at the configured opening hour is received, the strategy submits a buy stop above the ask and a sell stop below the bid. Both orders are offset by the `Distance (pips)` parameter converted to price units.
2. **Protective orders** – `StartProtection` automatically attaches stop-loss and take-profit protection using the pip distances defined in the parameters. `ManageRisk` doubles as a safeguard, closing any residual position if a completed candle shows the thresholds have been crossed.
3. **Session shutdown** – When the closing hour arrives, the strategy cancels any remaining pending orders and forcefully exits open trades regardless of profit or loss. This reproduces the original expert’s behaviour of resetting at the end of the session.
4. **Digit-aware pip size** – The pip multiplier emulates the MetaTrader implementation by multiplying the price step by ten for symbols quoted with three or five decimal places (e.g., JPY or 5-digit FX pairs). This keeps legacy inputs consistent across brokers.

The default candle type is 30-minute bars to stay under the original restriction of periods shorter than H1. Any other time frame can be used, as long as the resulting hourly timestamps match the desired session hours.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Opening Hour` | Hour (0-23) when the strategy will place the pair of stop orders. | 9 |
| `Closing Hour` | Hour (0-23) when all orders are cancelled and positions are closed. | 2 |
| `Distance (pips)` | Offset, in pips, between current price and the pending stop entries. | 20 |
| `Stop Loss (pips)` | Pip distance for the protective stop once a position is open. | 20 |
| `Take Profit (pips)` | Pip distance for the profit target once a position is open. | 500 |
| `Order Volume` | Quantity used when placing each pending stop order. | 0.1 |
| `Candle Type` | Time frame that drives the hourly schedule. | 30-minute TimeFrame |

All parameters can be optimised. Pip-based inputs are converted internally using the instrument’s price step so they remain portable between FX symbols with different decimal precision.

## Daily workflow

1. **At every candle close** the strategy checks whether the stop-loss or take-profit distance has been hit. If so, it closes the active position at market.
2. **When the closing hour is reached** it cancels any unfilled pending orders and exits the position, ensuring the book is flat before the next session.
3. **When the opening hour is reached** (and the strategy is flat) it cancels old orders just in case and submits a fresh sell stop below the bid and a buy stop above the ask. The orders are mirrored around the spread so either breakout can be captured.
4. **Throughout the session** the platform-level protection created by `StartProtection` keeps a stop-loss and take-profit attached, acting immediately if intrabar price action hits the thresholds.

## Usage notes

- Use instruments whose tick size represents a single “point” so that the pip adjustment mirrors the original expert. Exotic tick sizes may require manual tuning of the distance parameters.
- The logic assumes one trading cycle per day. If you use intraday data with multiple opening/closing matches, adjust the hours accordingly.
- Because all actions happen on candle completion, select a candle size that matches how often you want to evaluate the schedule. For example, hourly candles provide the same cadence as the MetaTrader version.
- The strategy only places new pending orders when the position is flat, avoiding overexposure if a breakout trade is still active during the next opening hour.

## Differences from the MQL version

- Protective exits are handled via `StartProtection` plus explicit checks, leveraging StockSharp’s high-level API instead of direct stop-loss assignment on the pending order ticket.
- Bid/ask prices are read from `Security.BestBid` and `Security.BestAsk`. If those quotes are unavailable, the candle close is used as a fallback reference.
- Market orders are used to liquidate positions at the closing hour for simplicity and to avoid broker-specific behaviours.
