# Elite eFibo Trader Strategy

## Overview
The **Elite eFibo Trader Strategy** is a conversion of the MQL5 expert advisor "Elite eFibo Trader". It implements a Fibonacci-based averaging grid that opens an initial market position and layers additional stop orders at fixed distances. The strategy operates on tick data and automatically manages trailing stops as the grid expands.

## How It Works
1. When no positions or pending orders are active and trading is allowed, the strategy starts a new cycle in the selected direction (buy or sell).
2. The first order is sent at market using the volume configured for `LotsLevel1`. Thirteen additional stop orders are placed at multiples of `LevelDistance` from the current price. Their volumes follow the Fibonacci sequence defined by `LotsLevel2` … `LotsLevel14`.
3. Each executed order sets an individual stop level `StopLossPoints` away from its entry price. The highest (for long positions) or lowest (for short positions) of these stops becomes the active trailing level for every open position.
4. If price hits the trailing level, the entire position is closed and all remaining pending orders are cancelled.
5. Unrealized profit is monitored in account currency. Once it reaches `MoneyTakeProfit`, the grid is closed. Depending on `TradeAgainAfterProfit`, the strategy either restarts automatically or waits for manual reactivation.

The strategy requires tick-level market data through `SubscribeTrades()` and expects that only one direction (`OpenBuy` xor `OpenSell`) is enabled at a time.

## Parameters
- `OpenBuy` – enables the long-only version of the grid.
- `OpenSell` – enables the short-only version of the grid.
- `TradeAgainAfterProfit` – automatically starts a new cycle after taking profit.
- `LevelDistance` – spacing between pending orders, measured in security price steps.
- `StopLossPoints` – stop-loss distance from each entry, measured in price steps.
- `MoneyTakeProfit` – unrealized profit target expressed in account currency.
- `LotsLevel1` … `LotsLevel14` – individual volumes for each grid level. Default values follow the Fibonacci sequence (1, 1, 2, 3, 5, …, 377).

## Trading Logic Details
- Price offsets are calculated with the instrument `PriceStep`; if it is zero the strategy will not place orders.
- Only one trading cycle is active at a time. All pending orders are created at cycle start and remain until executed or explicitly cancelled.
- Trailing stops are recalculated whenever a new grid level is filled or portions of the position are closed. This ensures that every order shares the best available protective level.
- Profit control is based on floating PnL derived from `Position`, `PositionPrice`, `PriceStep`, and `StepPrice`.
- When `TradeAgainAfterProfit` is disabled, the strategy stays inactive after reaching the money target until the parameter is toggled back manually.

## Usage Notes
- Configure the correct direction before starting (long or short). Enabling both directions simultaneously prevents the grid from launching.
- Adjust level distances and volumes according to the instrument’s volatility and contract size. Large Fibonacci volumes create aggressive scaling and should be tested carefully on historical data.
- Ensure the trading account and broker support stop orders at the calculated price levels; otherwise, orders may be rejected.
