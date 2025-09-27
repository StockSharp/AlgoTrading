# Close Orders Risk Control Strategy

## Overview
The **Close Orders Strategy** is a risk-management utility that mirrors the behaviour of the original MQL expert advisor *CloseOrders.mq4*. It continuously monitors the floating profit and loss of open positions and automatically liquidates matching orders once either the profit target or the cut-loss threshold is reached. This makes it suitable for protecting a portfolio or synchronising exits across multiple strategies.

## How it works
1. The strategy subscribes to a configurable candle series (1-minute by default) and evaluates the current floating PnL whenever a candle closes.
2. Floating PnL is calculated for the active portfolio positions. When a magic number is provided, only positions whose internal `StrategyId` matches the configured value are included.
3. If the floating profit is equal to or greater than the target amount, every matching order and position is closed.
4. If the floating profit drops below the configured cut-loss (a negative number), the same liquidation routine is triggered to minimise further losses.
5. Active orders that satisfy the magic-number filter are cancelled before flattening positions to ensure no new exposure is opened during liquidation.

The liquidation routine keeps running until all matching positions are flat, ensuring partial fills are handled gracefully.

## Parameters
| Parameter | Description |
| --- | --- |
| **Target Profit Money** | Floating profit (in account currency) that triggers the liquidation of matching orders. Must be greater than zero. |
| **Cut Loss Money** | Negative floating PnL (in account currency) that forces liquidation. A value of `0` disables the loss-based exit. |
| **Magic Number** | Optional strategy identifier. Leave empty to manage every open position; otherwise only positions whose `StrategyId` equals the supplied value are affected. |
| **Candle Type** | Candle series used to trigger periodic profit checks. Adjust the timeframe when higher-frequency monitoring is required. |

## Implementation notes
- The MQL magic number concept is mapped to the `UserOrderId`/`StrategyId` fields in StockSharp. Ensure the strategies that should be managed use the same identifier.
- Tabs are used for indentation, and the file follows the common structure requested for converted strategies.
- The strategy cancels pending orders before sending market orders to flatten exposure, preventing immediate re-entry.
- Start protection can be added if the strategy is combined with live trading components that need emergency handling.

## Usage tips
- Deploy the strategy alongside trading strategies that set a custom `StrategyId` to centralise exit logic.
- Adjust the `Candle Type` parameter to balance responsiveness and resource usage; shorter timeframes provide faster reaction to PnL changes.
- Combine the utility with alerts to receive notifications whenever automated liquidation is executed.
