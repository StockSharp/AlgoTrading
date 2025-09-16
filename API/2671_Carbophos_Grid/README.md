# Carbophos Grid Strategy

## Overview
The Carbophos Grid Strategy is a direct conversion of the "Carbophos" MetaTrader 5 expert advisor. It continuously maintains a symmetric ladder of buy and sell limit orders around the current bid/ask prices. The strategy monitors the aggregated floating profit of the entire grid and closes all exposure once either the desired profit target or the maximum tolerated drawdown is reached. After the position is flattened and no working orders remain, the ladder is rebuilt automatically.

## Trading Logic
1. When the strategy starts and there are no active orders or open positions, it calculates the grid spacing in price units based on the configured step in pips and the instrument's price precision. Five (configurable) sell limit orders are placed above the best bid and the same number of buy limit orders are placed below the best ask.
2. If any order is filled, the resulting position is monitored tick-by-tick using Level1 data. Floating PnL is computed from the difference between the current exit price (bid for long positions, ask for short positions) and the volume-weighted average entry price.
3. Once the floating profit exceeds the configured target, or the floating loss breaches the protection threshold, the strategy submits a market order to close the open position and cancels all remaining limit orders. The state flag is cleared so that the ladder will be rebuilt on the next price update.
4. If all orders are filled but the net position returns to zero (for example, because the market reverses through the grid), the next Level1 update triggers a new ladder placement.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `ProfitTarget` | Floating profit (money) that triggers closing the entire grid. |
| `MaxLoss` | Floating loss (money) that forces an emergency exit. |
| `StepPips` | Distance between consecutive grid levels expressed in pips. Internally converted to price units using the symbol's tick size and decimal precision. |
| `OrdersPerSide` | Number of limit orders placed above and below the current market price. |
| `OrderVolume` | Volume for every grid order. |

All parameters support optimization ranges to simplify experimentation in the StockSharp optimizer.

## Risk Management and Protections
The strategy uses the built-in `StartProtection()` hook and applies hard monetary stop/profit levels at the strategy level. The floating PnL calculation relies on the instrument's `PriceStep` and `StepPrice` settings. When either threshold is met, the strategy closes the position with a market order and cancels every working limit order before resetting the internal grid flag.

## Conversion Notes
- The original MQL5 expert advisor adjusted pip values for three- and five-decimal Forex symbols. The StockSharp port replicates this behavior by multiplying the exchange `PriceStep` by 10 whenever the security exposes three or five decimals.
- MetaTrader aggregates position profit, commission, and swap per magic number. In StockSharp the floating PnL is recomputed from the weighted average entry price and the current bid/ask price, so explicit commission handling is not required.
- Order placement, cancellation, and position management are implemented via the high-level `Strategy` API (`BuyLimit`, `SellLimit`, `CancelActiveOrders`, `BuyMarket`, `SellMarket`) as required by the project guidelines.
- The grid is refreshed exclusively from Level1 updates, replicating the "OnTick" behaviour of the original code without introducing custom timers or collections.

## Usage
1. Assign the desired `Security` and `Portfolio` to the strategy instance before starting it.
2. Optionally adjust the parameters to match the target instrument's volatility and risk tolerance.
3. Start the strategy. It immediately subscribes to Level1 data, builds the first grid once both bid and ask prices are available, and keeps managing exposure automatically.
4. Monitor the log for messages such as "Profit target reached" or "Maximum loss reached" to know when the grid has been reset.

Ensure that the selected instrument provides Level1 updates with best bid and ask prices; otherwise the ladder will not be built.
