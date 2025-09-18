# Basket Close Utility

## Overview
The Basket Close Utility strategy mirrors the behaviour of the MetaTrader expert "Basket Close 2". It continuously monitors the floating profit and loss of every open position in the connected portfolio. When either a configurable profit objective or a loss limit is reached, the strategy sends market orders to flatten **all** exposures in every instrument involved. Optionally, it can automatically open a small test position whenever the book is flat, which is useful inside backtests for validating that the protection logic works as expected.

## Parameters
| Name | Description |
| --- | --- |
| `LossMode` | Chooses whether the loss guard compares percentages or currency values. |
| `LossPercentage` | Negative percentage drawdown (expressed in absolute value) that triggers the loss exit when `LossMode` is `Percentage`. |
| `LossCurrency` | Floating loss in account currency that triggers the exit when `LossMode` is `Currency`. |
| `ProfitMode` | Chooses whether the profit objective compares percentages or currency values. |
| `ProfitPercentage` | Percentage gain that closes all positions when `ProfitMode` is `Percentage`. |
| `ProfitCurrency` | Floating profit in account currency that closes all positions when `ProfitMode` is `Currency`. |
| `CandleType` | Timeframe used to trigger periodic checks of the floating profit and loss. |
| `EnableTestOrders` | When enabled the strategy sends a single market buy order whenever no positions are open. |
| `TestOrderVolume` | Trade size used when the optional test order is active. |

## Trading Logic
1. Subscribe to the configured candle series and run the evaluation only when a candle is fully finished, matching the behaviour of the original EA that works on closed bars.
2. Aggregate the floating profit and loss of every open position. If the portfolio object exposes a combined floating profit it is used; otherwise the strategy sums the PnL of each position.
3. Compute the percentage change relative to the current account balance captured at start-up.
4. Trigger the loss routine when the floating PnL breaches the configured limit. Trigger the profit routine when the floating PnL or the percentage gain reaches the profit target.
5. Once triggered, keep sending market orders until every open position across the entire portfolio is closed. This includes the main security as well as positions opened by child strategies.
6. Optionally send a market order to reopen exposure (for testing) after the book becomes flat.

## Notes
- The MetaTrader expert displayed textual information on the chart. In StockSharp the important figures are logged through `LogInfo` instead.
- Swap and commission adjustments from the original script are implicitly included inside the floating PnL reported by the portfolio or individual positions.
- The percentage thresholds use the account balance captured when the strategy starts. Adjust the limits when running long sessions if the equity base changes substantially.
- When the optional test order is enabled, the helper order is reissued whenever the previous exposure has been closed by the profit or loss guard.
