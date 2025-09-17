# Trading Panel Strategy (ID 3468)

## Overview
The **TradingPanelStrategy** is a manual order-entry helper converted from the MQL5 expert advisor *EA_TradingPanel*. It exposes programmatic methods that replicate the original on-chart panel: a single action can submit multiple market orders, automatically attach stop-loss and take-profit distances measured in pips, and optionally select a custom security to trade. The defaults mirror the source EA (one trade, 2-pip stop, 10-pip take, 0.01 volume).

Unlike the graphical panel, this StockSharp port focuses on automation-friendly entry points. Callers (for example, a custom UI or script) can trigger `PlaceBuyOrders()` or `PlaceSellOrders()` whenever required, while the strategy takes care of volume normalisation, price rounding and protective order placement.

## Parameters
| Name | Description | Notes |
| ---- | ----------- | ----- |
| `TradeCount` | Number of market orders sent per action. | Ensures at least zero. Default `1`. |
| `StopLossPips` | Stop-loss distance in pips. | Zero disables stop creation. Default `2`. |
| `TakeProfitPips` | Take-profit distance in pips. | Zero disables target creation. Default `10`. |
| `VolumePerTrade` | Volume for each individual market order. | Rounded via `Security.VolumeStep`. Default `0.01`. |
| `TargetSecurity` | Optional override for the traded instrument. | Falls back to `Strategy.Security` when null. |

All parameters are exposed through `StrategyParam<T>` so they support optimisation and runtime reconfiguration from StockSharp UI.

## Execution Flow
1. Resolve the active security (`TargetSecurity` or `Strategy.Security`).
2. Derive the pip size from instrument metadata: `PriceStep` multiplied by 10 when the instrument has 3+ decimals, identical to the MQL logic that multiplies for symbols with 3 or 5 digits.
3. Obtain the latest reference price (best bid/ask, falling back to last trade) and round it with `Security.ShrinkPrice`.
4. Compute the desired volume: `TradeCount × VolumePerTrade`, align it with exchange limits (`MinVolume`, `MaxVolume`, `VolumeStep`) and adjust for an opposite open position so that one action can both flatten and reverse.
5. Submit a market order via `BuyMarket` or `SellMarket`.
6. Create protective orders (stop and limit) using the pip offsets, again normalised to the exchange tick size.
7. Cancel obsolete protective orders whenever the position flips or the strategy stops.

## Protective Order Logic
- Long entries place a `SellStop` for the stop-loss and a `SellLimit` for the take-profit.
- Short entries place a `BuyStop` for the stop-loss and a `BuyLimit` for the take-profit.
- Each protective order covers the newly requested panel volume (the same amount as a single action on the original MQL panel).
- Orders are cancelled automatically in `OnStopped`, `OnReseted` and whenever the opposite side is triggered.

## Usage Notes
- Assign `Strategy.Security` in the host application or provide a `TargetSecurity` before calling the panel methods; otherwise no trades will be submitted.
- Invoke `PlaceBuyOrders()` to replicate the MQL “BUY” button and `PlaceSellOrders()` for the “SELL” button.
- Prices rely on live market data. If neither best bid/ask nor last trade is available the strategy logs an error and skips order submission.
- The helper calls `StartProtection()` in `OnStarted` to guard against stale positions after restarts.
- When the instrument metadata does not include `PriceStep`, the pip size defaults to `0.0001` (one pip for most FX symbols); set `PriceStep` explicitly if your broker uses alternative increments.

## Differences Compared to the MQL Panel
- There is no embedded graphical UI. Integrators are expected to build their own interface or trigger the public methods from external logic.
- Protective orders are aggregated per action instead of per individual MT5 ticket. The resulting net exposure matches the MT5 behaviour while keeping the StockSharp implementation concise.
- Volume and price validation follows StockSharp conventions (`Security.ShrinkPrice`, `VolumeStep`, `MinVolume`, `MaxVolume`). This avoids rejected orders on venues with strict increments.
- Execution logging is provided through `LogInfo` and `LogError` to aid monitoring in StockSharp terminals.

## Getting Started
1. Instantiate the strategy, assign portfolio and security (or set `TargetSecurity`).
2. Start the strategy so that `StartProtection()` arms the internal safeguards.
3. Call `PlaceBuyOrders()` or `PlaceSellOrders()` based on user input or automated triggers.
4. Monitor the log for confirmation messages and manage additional UI logic as required.

This manual trading panel conversion offers a lightweight yet faithful reproduction of the original MT5 expert advisor, adapted to StockSharp’s high-level strategy framework.
