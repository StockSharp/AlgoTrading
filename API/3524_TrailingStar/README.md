# Trailing Star Trailing Stop Collection

## Origin

The original MetaTrader 5 package "Trailing Star" by Pham Ngoc Vinh provides two expert advisors:

* **Trailing Star on Point** – activates a trailing stop when the position earns a configurable amount of points over the entry price.
* **Trailing Star on Price** – starts trailing when the market trades beyond a user-defined price level.

Both advisors continuously monitor all open positions and modify the built-in stop-loss level using `PositionModify` to keep the stop a fixed distance away from the current bid or ask price.

## StockSharp adaptation

This folder contains two StockSharp strategies that replicate the MetaTrader behaviour using the high-level API:

* `TrailingStarPointStrategy` – measures the activation and trailing distance in MetaTrader "points" (pips). The strategy waits until the best bid/ask price exceeds the weighted average entry price by the configured number of points and then re-registers a stop order to follow the price.
* `TrailingStarPriceStrategy` – activates once the market crosses a fixed price threshold. After activation, the strategy trails the stop order by the specified number of points.

Both implementations work with **Level1** data and do not rely on custom collections or indicator buffers. They normalize prices and volumes through the base `Strategy` helpers and only re-register the stop when it improves the risk profile (stop moves upward for longs and downward for shorts), matching the original MetaTrader checks.

### Tracking entry prices

`TrailingStarPointStrategy` reconstructs the entry price of the active position from fill notifications (`OnNewMyTrade`).

* Buy fills first reduce any remaining short exposure before updating the long average entry price.
* Sell fills first reduce the long exposure before updating the short average entry price.
* When the net position returns to zero, all tracking data and stop orders are reset.

This logic emulates the MetaTrader loop over `PositionsTotal()` that used `POSITION_PRICE_OPEN` for the current position.

`TrailingStarPriceStrategy` does not need to track fill prices because the activation price is provided by the user.

### Working with Level1 data

The strategies subscribe to Level1 updates (`SubscribeLevel1().Bind(...)`) to obtain the best bid and ask values. These prices replace the MetaTrader `latest_price.bid` and `latest_price.ask` used in the original advisors. All trailing calculations are performed once the subscription delivers fresh data. If no Level1 information is available, the trailing logic skips the update, ensuring safe behaviour on illiquid instruments.

### Stop order management

The original MetaTrader code called `PositionModify` to update the existing stop-loss. In StockSharp the strategies maintain a single protective order:

1. If no stop order exists, the strategy places a new stop (`SellStop` for longs, `BuyStop` for shorts) at the required price.
2. If the protective order exists and remains active, the strategy re-registers it only when the new price is more favourable.
3. Completed or failed protective orders are recreated automatically.

When the position is closed, all internal state and stop orders are cancelled, mirroring the MetaTrader behaviour that skipped closed positions.

## Parameters

| Strategy | Parameter | Description |
| --- | --- | --- |
| `TrailingStarPointStrategy` | `EntryPointPips` | Minimum profit in MetaTrader points required before the trailing stop is activated. |
| | `TrailingPointPips` | Distance (in MetaTrader points) maintained between the current price and the stop order. |
| `TrailingStarPriceStrategy` | `EntryPrice` | Market price that must be breached before the trailing stop starts following the price. |
| | `TrailingPointPips` | Distance (in MetaTrader points) maintained between the current price and the stop order. |

All parameters are exposed via `StrategyParam<T>` with `SetDisplay` metadata to integrate seamlessly with the StockSharp UI and optimizer.

## Usage notes

1. Assign the target security and portfolio before starting the strategy.
2. Ensure the chosen data source supplies best bid and best ask Level1 updates.
3. Configure the parameters according to the instrument’s point size. The strategies internally convert MetaTrader points to actual price increments using the security’s `PriceStep` and decimal precision.
4. Start the strategy. It will automatically maintain trailing stops for the active position without placing new entries.

> **Important:** The strategies only manage existing positions. They do not open new trades. Combine them with other entry strategies or manual trading if automated trailing protection is required.

## Differences from the original MQL version

* Stop management is implemented through StockSharp protective orders (`SellStop`/`BuyStop`) instead of `PositionModify`.
* Position tracking relies on trade notifications rather than iterating over the MetaTrader position pool.
* The strategies operate on a single security defined by the StockSharp strategy instance, while the MQL version iterated over every open position globally. This aligns with StockSharp’s architecture and simplifies risk control.

These adjustments preserve the trading logic while embracing idiomatic StockSharp patterns.
