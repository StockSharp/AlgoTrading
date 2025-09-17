# RangeEA Strategy

## Overview

RangeEA is a limit-order grid strategy converted from the original MetaTrader expert advisor. The algorithm identifies the current
weekly trading range and populates it with a configurable number of pending limit orders. Each order uses dynamic stop-loss and
take-profit offsets scaled relative to the distance between the limit price and the current market price while also respecting
minimal distances expressed in points. Profits can be locked by closing the entire book once the portfolio equity grows by a
predefined percentage.

The implementation leverages StockSharp's high-level API: candles drive the decision logic, pending orders are managed with the
strategy helper methods, and risk controls are exposed as optimization-ready parameters.

## Trading Logic

1. Subscribe to two candle streams:
   - A user-defined timeframe (1-hour by default) that drives grid maintenance.
   - Weekly candles that are used to estimate the current trading range.
2. For every finished weekly candle, update the highest high and lowest low across the last two weeks. Their difference becomes
the active trading range.
3. On each finished trading candle:
   - Respect the configured trading window (`StartTradeHour` to `EndTradeHour`).
   - Optionally reset the grid at the beginning of each trading day.
   - If no pending limit orders exist, distribute new orders evenly between the range low and the range high.
   - After two orders have already been executed, replace the second-last fill with a new order at the same price when the grid
     shrinks to `NumberOfOrders - 2` items.
   - Continuously monitor the account equity and liquidate everything when the configured profit percentage is reached.
4. When the trading window closes and `CloseAllAtEndTrade` is enabled, cancel every pending order and exit existing positions.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Trading timeframe used to trigger grid maintenance. | 1 hour candles |
| `WeeklyCandleType` | Timeframe used to derive the range boundaries. | 1 week candles |
| `StartTradeHour` | Hour of day when new orders may be placed. | 0 |
| `EndTradeHour` | Hour of day when trading stops. | 24 |
| `CloseAllAtEndTrade` | Close all orders and positions outside of the trading window. | true |
| `MaxOpenOrders` | Maximum number of simultaneous orders and positions. | 5 |
| `NumberOfOrders` | Number of limit orders in the grid. | 10 |
| `OrderVolume` | Volume used for each order. | 0.01 |
| `ResetOrdersDaily` | Rebuild the grid at the start of each trading day. | true |
| `StopLossPoints` | Minimum stop-loss distance in points. | 60 |
| `TakeProfitPoints` | Minimum take-profit distance in points. | 60 |
| `StopLossMultiplier` | Multiplier applied to the dynamic stop-loss distance. | 3 |
| `TakeProfitMultiplier` | Multiplier applied to the dynamic take-profit distance. | 1 |
| `TargetPercentage` | Equity gain percentage that triggers liquidation. | 8 |

## Risk Management

- The strategy honours the `MaxOpenOrders` limit to keep the number of active orders and positions under control.
- Stop-loss and take-profit levels are always at least the configured number of points away from the entry and can optionally be
  extended by the multiplier parameters.
- The daily reset option prevents stale orders from being carried into a new session.
- A portfolio-level equity target allows the strategy to lock in profits by flattening the book.

## Notes

- Ensure the selected security provides weekly candles; otherwise the strategy cannot compute the range.
- When using instruments with non-standard price steps, adjust the point-based settings to match the underlying tick size.
- Optimizing `NumberOfOrders`, `OrderVolume`, and the stop/take multipliers helps adapt the grid to different levels of
  volatility.
