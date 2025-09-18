# Target EA Manager Strategy

## Overview
The **Target EA Manager Strategy** is a faithful StockSharp port of the MetaTrader expert *TargetEA_v1.5*. The strategy does not open new trades by itself. Instead, it constantly monitors the floating profit and loss of the orders that already belong to the strategy and, if required, liquidates positions and cancels pending orders when user defined thresholds are reached. The behaviour reproduces the "basket" management logic of the original expert: buy and sell orders may be evaluated independently or as a single combined basket.

The strategy subscribes to Level1 data (best bid and ask) and relies on the high level API for position closing and order cancellation. Real-time bid and ask quotes are translated into unrealized profit metrics for the open exposure.

## Key Features
- **Independent or combined baskets** – choose whether long and short orders are treated separately or together via `ManageBuySellOrders`.
- **Multiple target types** – thresholds may be expressed in pips, in account currency per lot, or as a percentage of the portfolio balance, matching the `TypeTargetUse` flag of the MQL version.
- **Dual-side triggers** – separate toggles for reacting to floating profits (`CloseInProfit`) and floating losses (`CloseInLoss`).
- **Pending order cleanup** – optional cancellation of buy and/or sell pending orders each time a basket is closed.
- **High level operations** – market exits are executed with `BuyMarket` / `SellMarket`, and pending orders are cancelled via the strategy order collection.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `ManageBuySellOrders` | `Separate` emulates two baskets (long and short), `Combined` fuses both sides. |
| `CloseBuyOrders` / `CloseSellOrders` | Enable liquidation for the respective side. |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | Cancel active pending orders after a basket closes. |
| `TypeTargetUse` | `Pips`, `CurrencyPerLot`, or `PercentageOfBalance` select the measurement applied to open PnL. |
| `CloseInProfit` / `CloseInLoss` | Activate profit or loss triggers. |
| `TargetProfitInPips`, `TargetLossInPips` | Thresholds in pips. When the instrument provides `PriceStep` the pip value is calculated as `priceDifference / PriceStep * (volume / VolumeStep)`. |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | Floating profit or loss per lot, multiplied by the current volume before comparison. |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | Percentage of the portfolio balance that must be reached before closing. The original expert compares raw floating profit to `Balance ± Balance * Percentage / 100`, and this port keeps that convention intact. |

## Behaviour
1. **State tracking** – executed trades update internal long and short volume totals and their weighted average prices. Hedged positions (both long and short) are therefore handled correctly.
2. **PnL calculation** – each Level1 update refreshes bid/ask values, from which pips and currency profits for both sides are computed.
3. **Target evaluation** – depending on the target mode and basket mode the corresponding thresholds are checked. Profit checks require values to be *greater than or equal* to the configured targets, while loss checks use *less than or equal* comparisons, matching the MQL logic.
4. **Basket liquidation** – when a condition is satisfied the strategy optionally cancels pending orders on that side and sends the necessary market order to flatten the open exposure.

The implementation intentionally avoids additional collections or indicator storage and relies on the StockSharp high level API, just like the original EA.
