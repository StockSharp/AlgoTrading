# Manual Position Tracking Panel Strategy

## Overview

The original MQL5 expert advisor provided a visual control panel that allowed a trader to manage up to five long and five short positions manually. Buttons inside the panel deleted existing take-profit levels, recalculated new take-profit prices from the entry, or moved them to break-even for the selected tickets. The StockSharp port automates these protective actions without the visual interface. The strategy monitors the aggregated position for the configured symbol and dynamically maintains a protective take-profit order that mirrors the panel workflow.

Key automation steps:

- Place a take-profit at the entry price plus/minus a configurable MetaTrader pip distance when a position appears.
- Optionally push the take-profit to the average entry price once the market moves in the favourable direction by the requested number of pips, effectively locking a break-even exit.
- Respect broker freeze/stop distances when they are published via Level1 data, or approximate them using the current spread and a user-controlled multiplier.
- Cancel the protective order whenever management is disabled or the position is closed, keeping the behaviour consistent with the "Delete TP" button from the panel.

The class relies exclusively on high-level StockSharp API methods (`SubscribeLevel1`, `SellLimit`, `BuyLimit`, `ReRegisterOrder`, etc.) and uses automatic volume/price normalisation so it can be attached to any instrument supported by the connector.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Take profit distance (pips)** | MetaTrader pip distance added to the entry price when creating the protective take-profit. |
| **Enable entry-based take profit** | Enables automatic placement of the take-profit derived from the entry price. When disabled the strategy only reacts to break-even requests. |
| **Enable break-even** | Moves the take-profit back to the average entry price once the break-even trigger is satisfied. |
| **Break-even trigger (pips)** | Minimal favourable movement (in MetaTrader pips) required before break-even is applied. A value of `0` applies it immediately. |
| **Manage long positions** | When `true` the long side of the aggregated position is processed. |
| **Manage short positions** | When `true` the short side of the aggregated position is processed. |
| **Remove take profit when disabled** | Cancels the protective order if management conditions are not satisfied (similar to the original Delete TP button). |
| **Log management actions** | Enables informational logging for every create, modify, or cancel action performed by the algorithm. |
| **Freeze distance multiplier** | Multiplier used to approximate freeze/stop distances from the current spread when the exchange does not publish explicit levels. |

## Signals and Execution Rules

1. On start-up the strategy subscribes to Level1 updates in order to track best bid/ask prices plus optional freeze and stop levels exposed by the gateway.
2. Whenever a new trade appears, the overall position changes, or new level1 data arrives, the strategy re-evaluates the protection logic.
3. If no position is open, any existing take-profit order is cancelled.
4. If a position is active and the corresponding side is enabled:
   - The base target is the entry price shifted by the configured take-profit distance (if enabled).
   - When break-even is enabled and the current market price has moved enough, the target is clamped to the average entry price.
   - The target is adjusted to respect freeze/stop distances by comparing it with the current market quote.
   - Price and volume are normalised via `PriceStep`/`VolumeStep`, then a limit order is registered or re-registered on the opposite side.
5. If the configuration disables management for the detected side, the existing take-profit is removed when **Remove take profit when disabled** is `true`.

## Risk Management Notes

- The algorithm only manages take-profit orders. Stop-loss levels, trailing logic, or partial exits are outside its scope.
- Because the original panel worked with MetaTrader "pips" (points), the strategy computes the pip size automatically from `PriceStep` and the instrument precision to remain compatible with Forex symbols.
- Level1 freeze/stop distances are respected when available. If the broker does not send them, the multiplier parameter lets the user create a safety buffer from the live spread, preventing rejected modifications.
- The strategy does not create new market entries; it is designed to be attached to discretionary or external trading systems that already manage order execution.

## Usage Tips

1. Attach the strategy to the instrument that you want to supervise and ensure the connector supplies Level1 information.
2. Configure the pip distance so that it matches the protective target you previously used inside MetaTrader.
3. Enable the break-even module when you want the protection to lock profits once a position becomes favourable. Leave the trigger at zero for an immediate break-even.
4. Disable management for a side (long or short) if you want to keep discretionary control over that direction.
5. Monitor the log output when **Log management actions** is active to verify that the orders are created or adjusted as expected.

