# ProfitLossTrailStrategy

## Overview

ProfitLossTrailStrategy is a risk-management helper converted from the MetaTrader expert advisor **ProfitLossTrailEA v2.30**. The strategy does not generate entries on its own. Instead, it supervises the currently open position on the configured security and automatically applies protective exits:

- initial stop-loss and take-profit levels;
- trailing stop management with optional activation distance and trailing step control;
- break-even protection with a configurable profit trigger and offset;
- ability to remove existing protective levels when the trader wants to manage them manually.

The behaviour closely matches the original EA's "basket" management mode: all orders of the same direction are treated as a single position and the protective levels are recalculated whenever exposure changes.

## Parameter reference

| Parameter | Description |
|-----------|-------------|
| **Manage As Basket** | When enabled (default), every fill in the same direction recalculates the average entry price and refreshes stop-loss/take-profit levels. Disable the flag to keep the initial levels after the first fill. |
| **Enable Take Profit** | Turns automatic take-profit handling on or off. |
| **Take Profit (pips)** | Distance in pips between the entry price and the take-profit target. |
| **Enable Stop Loss** | Turns automatic stop-loss handling on or off. |
| **Stop Loss (pips)** | Distance in pips between the entry price and the initial protective stop. |
| **Enable Trailing Stop** | Activates dynamic stop management once the position is in profit. |
| **Trailing Activation (pips)** | Minimum profit in pips required before the trailing stop can move. Use `0` to activate immediately. |
| **Trailing Stop (pips)** | Base trailing distance expressed in pips. |
| **Trailing Step (pips)** | Additional profit that must be gained before tightening the trailing stop further. |
| **Enable Break-Even** | Enables the break-even routine that moves the stop into profit after a trigger distance. |
| **Break-Even Trigger (pips)** | Profit distance that activates the break-even move. |
| **Break-Even Offset (pips)** | Extra offset added above (long) or below (short) the entry price when break-even activates. |
| **Remove Take Profit** | When set to `true`, any current take-profit value is cleared and no take-profit exits are issued. |
| **Remove Stop Loss** | When set to `true`, any current stop-loss value is cleared and no stop-loss or trailing exits are issued. |
| **Candle Type** | Candle series used to monitor price action. Trailing, break-even, and exit checks are evaluated on finished candles. |

## Usage notes

1. Attach the strategy to a security and ensure that orders are placed externally or by another strategy. ProfitLossTrailStrategy focuses purely on managing the open exposure.
2. Configure the pip-based parameters to match the instrument's pricing. Pip size is automatically derived from `Security.PriceStep`.
3. When both break-even and trailing stop are enabled, the break-even adjustment happens first. Subsequent trailing steps will only tighten the stop if the new level improves on the current protective price by at least the specified trailing step distance.
4. Setting **Remove Stop Loss** disables stop-loss, trailing, and break-even logic simultaneously, mirroring the original EA behaviour.
5. The strategy uses market orders (`BuyMarket`/`SellMarket`) to close positions when protective levels are reached.

## Conversion notes

- The MetaTrader "Order_By_Order" and "Same_Type_As_One" modes are represented by the **Manage As Basket** flag. Managing per-ticket stop levels is not supported in StockSharp, so the basket mode is applied by default.
- Magic number and comment filters from the original EA are not required; the strategy acts on the configured `Strategy.Security` only.
- Screen drawing, sound alerts, and timer-based UI refreshes were omitted because StockSharp already exposes diagnostics via logs and chart bindings.
