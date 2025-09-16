# Trading Boxing Strategy

## Overview
Trading Boxing Strategy recreates the manual order management panel from the original TradingBoxing expert advisor. Instead of on-chart buttons the StockSharp version exposes parameters that can be toggled from the strategy UI or automation. Each toggle immediately performs the requested action and then resets itself, giving you a convenient control surface for market entries, pending order placement and existing position cleanup.

The strategy does not rely on indicator logic or market data events. It simply coordinates order submission and cancellation for the security and portfolio assigned to the strategy instance.

## Parameters
### Volume configuration
- `BuyVolume` – quantity used when the *Open Buy Market* action is triggered. Must be positive.
- `SellVolume` – quantity used when the *Open Sell Market* action is triggered. Must be positive.
- `BuyStopVolume` – quantity for new buy stop orders.
- `BuyLimitVolume` – quantity for new buy limit orders.
- `SellStopVolume` – quantity for new sell stop orders.
- `SellLimitVolume` – quantity for new sell limit orders.

### Price configuration
- `BuyStopPrice` – activation price for buy stop orders.
- `BuyLimitPrice` – price for buy limit orders.
- `SellStopPrice` – activation price for sell stop orders.
- `SellLimitPrice` – price for sell limit orders.

### Action toggles
All action parameters are boolean switches. Setting a switch to `true` performs the corresponding task and the strategy sets it back to `false` in the same processing cycle.

- `CloseBuyPositions` – closes the current long exposure (if `Position > 0`).
- `CloseSellPositions` – closes the current short exposure (if `Position < 0`).
- `DeleteBuyStops` – cancels tracked buy stop orders.
- `DeleteBuyLimits` – cancels tracked buy limit orders.
- `DeleteSellStops` – cancels tracked sell stop orders.
- `DeleteSellLimits` – cancels tracked sell limit orders.
- `OpenBuyMarket` – sends a market buy order using `BuyVolume`.
- `OpenSellMarket` – sends a market sell order using `SellVolume`.
- `PlaceBuyStop` – registers a new buy stop order at `BuyStopPrice` with `BuyStopVolume` and stores it for later cancellation.
- `PlaceBuyLimit` – registers a new buy limit order at `BuyLimitPrice` with `BuyLimitVolume` and stores it for later cancellation.
- `PlaceSellStop` – registers a new sell stop order at `SellStopPrice` with `SellStopVolume` and stores it for later cancellation.
- `PlaceSellLimit` – registers a new sell limit order at `SellLimitPrice` with `SellLimitVolume` and stores it for later cancellation.

## Behaviour details
- Orders created through the pending order actions are tracked internally so that the delete actions can cancel them later. External orders that were not placed by this strategy are not affected.
- The strategy verifies that it is running and that both `Security` and `Portfolio` are assigned before executing any request. When a requirement is missing it logs a warning and ignores the toggle.
- Volume and price validation replicates the original panel’s safeguards: any non-positive amount triggers a warning and no order is sent.
- Closing actions operate on the net position maintained by the strategy. If a short needs to be covered the strategy sends a buy market order equal to `Math.Abs(Position)`; for a long position it sends a sell market order of the current `Position` value.

## Usage notes
1. Start the strategy with a valid portfolio and security.
2. Adjust volume and price parameters to match the instrument you trade.
3. Trigger manual actions by setting the required boolean parameter to `true`. The property automatically reverts to `false` after the action completes so the next trigger is ready immediately.
4. Use the delete toggles to clear previously placed pending orders whenever the trading plan changes.

Because the strategy is purely event-driven by user input, there is no requirement to subscribe to candles or quotes. It acts as a simple execution assistant, mirroring the flexibility of the original TradingBoxing interface within the StockSharp environment.
