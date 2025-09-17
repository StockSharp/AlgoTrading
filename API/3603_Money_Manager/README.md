# Money Manager Strategy

## Overview
The original MetaTrader 5 expert advisor "Money manager 1.0" supervises every open position and automatically closes it once an unrealized profit target or drawdown limit is reached. Both thresholds are expressed as a percentage of the account balance with additional allowances for trading costs (commission per lot and the current spread). No new positions are opened – the script works purely as a money management layer for positions created elsewhere.

This StockSharp port keeps the behaviour intact. The strategy monitors Level1 data to capture the latest bid/ask quotes and checks the floating PnL of the aggregated position on each update. When the configured profit percentage (plus estimated costs) is exceeded, the strategy sends a `ClosePosition()` request. If the PnL drops below the allowed loss threshold, the position is closed as well. Disabling either rule is possible through the corresponding parameters, exactly like in the MQL version.

## Migration notes
- The MetaTrader function `AccountInfoDouble(ACCOUNT_BALANCE)` has been mapped to `Portfolio.CurrentValue`. It represents the current portfolio value in StockSharp and is the closest analogue to the MetaTrader balance figure that the script used when estimating thresholds.
- Bid/ask quotes are received via a `SubscribeLevel1()` subscription. The port calculates the spread from these values, mirroring the original `SymbolInfoDouble(_Symbol, SYMBOL_ASK/BID)` calls.
- Position profit is evaluated using the average entry price from `PositionPrice` and the latest close price inferred from the best bid (long) or best ask (short). This reflects `POSITION_PROFIT` in the source expert and preserves sign conventions for long and short trades.
- Strategy parameters are exposed with `StrategyParam<T>` so they can be optimised or modified through the StockSharp UI.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `ProfitDealEnabled` | `bool` | `true` | Enables the profit-based auto-closing rule. |
| `LossDealEnabled` | `bool` | `true` | Enables the loss-based auto-closing rule. |
| `ProfitPercent` | `decimal` | `5` | Percentage of the current portfolio balance that must be earned before locking profits. |
| `LossPercent` | `decimal` | `5` | Maximum loss as a percentage of the current balance before positions are liquidated. |
| `LotCommission` | `decimal` | `7` | Commission per lot added to both thresholds. |
| `LotSize` | `decimal` | `0.1` | Reference lot size used when translating the commission and spread into cash adjustments. |

## Usage
1. Attach the strategy to a security and portfolio that already host open trades or will be managed by another strategy.
2. Adjust the cost-related parameters (`LotCommission`, `LotSize`) to match the broker's contract specifications.
3. Define the acceptable profit and loss percentages. Both can be optimised thanks to `SetCanOptimize(true)` flags.
4. Start the strategy. It will continuously watch Level1 updates and close the position whenever the configured money management limits are hit.

The port is intentionally minimalistic: it does not alter volumes, submit new orders or interfere with other trade generation logic. Its only responsibility is to protect existing positions according to the thresholds inherited from the MetaTrader expert.
