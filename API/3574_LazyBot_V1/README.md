# LazyBot V1 Strategy

## Overview

LazyBot V1 is a daily breakout strategy converted from the original MetaTrader 5 expert advisor. Every trading day it places a pair of pending stop orders around the previous day's price range and uses a trailing stop to protect open positions. The conversion leverages the high-level StockSharp API with candle subscriptions and automatic order management.

## Trading Logic

1. Wait for a completed candle of the configured timeframe (daily by default).
2. On a new day, optionally ensure the current server time is inside the allowed trading window and skip weekends.
3. Cancel any existing breakout pending orders created by the strategy.
4. Place a buy stop above the previous day's high and a sell stop below the previous day's low. The `Breakout Offset (pips)` parameter adds extra distance to both breakout levels.
5. When either order is triggered, keep the protective stop-loss at a fixed distance and trail it whenever the price advances in the trade's favor by more than the configured pip distance.
6. Recompute volume for the next orders using either a fixed lot size or the risk-based sizing module.

## Parameters

| Name | Description |
| --- | --- |
| Candle Type | Timeframe used to gather the reference candles (daily by default). |
| Bot Name | Value written into order comments for easier tracking. |
| Stop Loss (pips) | Distance used for both the initial and trailing stop. |
| Breakout Offset (pips) | Extra distance applied to the previous high/low when placing the pending orders. |
| Max Spread (pips) | Maximum allowed spread before creating new breakout orders. Set to 0 to disable the check. |
| Use Trading Hours | Enables the start hour filter similar to the original EA. |
| Start Hour | First hour (inclusive) when new orders may be placed. |
| End Hour | Hour at which new orders stop being scheduled. When equal to the start hour the filter acts as a simple lower bound. |
| Use Risk % | Enables risk-based volume calculation. |
| Risk % | Percentage of the portfolio equity used to size positions when `Use Risk %` is enabled. |
| Fixed Volume | Fixed order volume used when risk sizing is disabled. When zero the strategy falls back to the global `Volume` property (defaults to 0.01). |

## Risk Management

* The trailing stop mirrors the MetaTrader trailing logic by keeping the stop loss `Stop Loss (pips)` away from the best bid/ask and only tightening when a better price is reached.
* The spread filter protects the strategy from submitting new breakout orders when the market is too wide.
* Risk-based sizing divides the allowed monetary risk (`equity * Risk %`) by the stop distance expressed in price units and never goes below the fixed lot size.

## Additional Notes

* Order comments follow the format `BotName;SymbolId;YYYYMMDD`, which makes it easy to distinguish pending orders created on different days.
* The strategy subscribes to Level1 data to evaluate the current spread for the filter and to trail stops with the latest bid/ask values.
* Trailing stops are reapplied on every candle update and immediately after fills to match the original EA behavior.
