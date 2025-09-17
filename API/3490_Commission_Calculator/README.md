# Commission Calculator Strategy

## Overview
The **Commission Calculator Strategy** is a utility strategy that mirrors the original MetaTrader script. It sends a single discretionary order using the selected execution mode (market, limit, or stop) and measures the broker commission applied to every resulting fill. The strategy stores the cumulative commission and prints a final report with the starting balance, total fees, and fee-adjusted balance when it finishes.

Unlike conventional signal-driven strategies, no market data or indicators are required. The strategy focuses on automated fee accounting for manual or semi-manual executions.

## Trading Logic
1. When the strategy starts, it captures the initial portfolio balance and configures the default trade volume.
2. Optional protective stop-loss and take-profit levels are activated through `StartProtection` when both the entry price and target prices are valid. The distances are calculated in absolute price units, mimicking the MQL implementation.
3. The configured order mode is executed exactly once. If parameters are inconsistent (for example, missing entry price for limit orders), the strategy logs the issue and skips sending the order.
4. Every own trade received through `OnNewMyTrade` is processed to calculate the commission fee using the configured percentage rate.
5. The strategy aggregates all commissions, remembers the latest fee, and logs a detailed summary on stop.

The implementation assumes that the broker fee is proportional to `price × volume × commissionRate / 100`. Adjust the rate to match the venue being modeled.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Quantity` | `0.001` | Trade volume sent by helper methods (`BuyMarket`, `SellLimit`, etc.). |
| `EntryPrice` | `31365` | Price used for limit or stop orders and for calculating protective distances. |
| `StopLossPrice` | `31200` | Price that defines the stop-loss distance. A non-positive distance disables the stop-loss protection. |
| `TakeProfitPrice` | `32100` | Price that defines the take-profit distance. A non-positive distance disables the take-profit protection. |
| `CommissionRate` | `0.04` | Commission rate expressed as a percentage of traded notional. |
| `Mode` | `None` | Order type to execute when the strategy starts. Options: `None`, `MarketBuy`, `MarketSell`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`. |

## Notes and Best Practices
- Start the strategy on a portfolio that supports manual order placement; no data subscriptions are required.
- Ensure that the broker commission model matches the `CommissionRate` parameter to avoid underestimating or overestimating fees.
- For pending orders, set `EntryPrice` to a valid level before launching the strategy; otherwise the order is not submitted.
- When protective levels are enabled, the strategy instructs the connector to use market exits upon trigger to closely mimic the original MQL behavior.

## Result Reporting
When `OnStopped` is invoked, the strategy logs:
- Initial balance snapshot (taken when the strategy started).
- Aggregated brokerage fees for all processed trades.
- Final balance adjusted by subtracting the accumulated fees.

This makes the strategy well suited for fast what-if analyses and for validating broker commission schedules during backtests.
