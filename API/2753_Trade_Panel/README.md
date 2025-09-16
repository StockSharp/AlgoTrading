# Trade Panel Strategy

## Overview
This strategy reproduces the trading actions of the original MetaTrader "TradePanel" expert advisor using the StockSharp high-level API. The original script provided a graphical panel that allowed a trader to submit market orders, close positions in several modes, and manage basic protective levels. In the StockSharp conversion the graphical interface is replaced by strategy parameters, while the execution flow and risk controls follow the same intent as in the MQL version.

## Trading Logic
1. **Market data feed** – the strategy subscribes to Level1 data for the configured security and stores the last trade price, best bid, and best ask. These values are used to evaluate stop-loss and take-profit thresholds as well as unrealized profit and loss for close decisions.
2. **Manual commands** – three boolean parameters (`BuyRequest`, `SellRequest`, `CloseRequest`) emulate the buttons from the TradePanel UI. When any of them is set to `true`, the strategy sends the corresponding market order and immediately resets the flag to `false`. Requests are processed only when the connection is online and both `Security` and `Portfolio` are assigned.
3. **Close modes** – the `CloseMode` enumeration mirrors the MQL panel behaviour:
   - `CloseAll` closes the entire net position.
   - `CloseLast` closes up to `OrderVolume`, which mimics closing the most recent trade size.
   - `CloseProfit` closes the full position only if the current price is favorable relative to the average entry price (`PositionPrice`).
   - `CloseLoss` closes the full position only if the trade is losing.
   - `ClosePartial` closes `PartialCloseVolume`, allowing the trader to scale out manually.
4. **Protective logic** – optional stop-loss and take-profit offsets, expressed in price points, are converted using `Security.PriceStep`. When the current price moves beyond either threshold the strategy sends a market order to flatten the entire position. A small internal flag avoids submitting duplicate protective orders until the position size changes.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Base volume for new manual market orders and for the `CloseLast` mode. |
| `StopLossPoints` | Stop-loss distance in price points. Set to `0` to disable. |
| `TakeProfitPoints` | Take-profit distance in price points. Set to `0` to disable. |
| `PartialCloseVolume` | Volume closed when `CloseMode` is `ClosePartial`. |
| `CloseMode` | Enumerated manual close behaviour (`CloseAll`, `CloseLast`, `CloseProfit`, `CloseLoss`, `ClosePartial`). |
| `BuyRequest` | Set to `true` to send a buy market order (auto-resets). |
| `SellRequest` | Set to `true` to send a sell market order (auto-resets). |
| `CloseRequest` | Set to `true` to trigger the selected close mode (auto-resets). |

## Differences from the MQL Version
- There is no on-chart user interface, painting tools, sound management, or speedometer. All interactions are handled through parameters.
- Orders operate on the net position managed by StockSharp, so multiple MetaTrader tickets are represented as a single aggregated position.
- Protective levels are executed with market orders upon threshold violation instead of modifying individual MetaTrader orders.
- Timer-based UI refresh and mouse events from the MQL panel are intentionally omitted because they are irrelevant for automated backtesting.

## Usage Notes
- Ensure that Level1 data is available; otherwise protective logic cannot evaluate prices.
- The manual request parameters are polled whenever new Level1 data arrives, so actions may wait for the next tick in a silent market.
- `StopLossPoints` and `TakeProfitPoints` rely on `Security.PriceStep`. Configure the security metadata so that price steps match the instrument used in MetaTrader.
- The strategy performs simple validity checks (connection online and required properties assigned) before placing manual orders, mirroring the safety checks from the original panel.
