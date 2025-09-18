# Dynamic Stop Loss

## Overview
The original MetaTrader expert advisor "Dynamic Stop Loss" does not open new trades on its own. Instead it watches existing market positions and, once a new candle appears, repositions the protective stop-loss so that it stays at a fixed distance behind the latest price. The StockSharp port keeps the same behaviour: every completed bar triggers a recalculation of the protective stop for whichever side is currently open. If no position exists, the strategy simply idles until a new position is detected.

## How it works
1. The strategy subscribes to candles defined by the `Candle Type` parameter (default 1-minute timeframe).
2. When a candle closes the close price is multiplied by the user-selected point distance. The distance is converted from MetaTrader-style points into an absolute price delta via `Security.PriceStep` (fallback to `Security.Step`, then to `1`).
3. If a long position is open the strategy cancels any existing stop order and places a new sell stop at `Close - Distance`.
4. If a short position is open the stop is moved to `Close + Distance` using a buy stop order.
5. When the position is closed (manually or by the stop filling) the trailing order is cancelled to avoid stale protection orders.

This produces the same constantly re-anchored stop distance as the MQL version, meaning the stop can move both closer to and further from the market as candles fluctuate.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `StopLossPoints` | `800` | Distance between the market price and the protective stop measured in instrument points. The value is multiplied by `Security.PriceStep` (fallback to `Security.Step`, then `1`) before being applied to the close price. Set to `0` to disable stop management. |
| `CandleType` | `TimeFrameCandle(00:01:00)` | Candle type that defines when the stop is recalculated. Choose a timeframe matching the chart used in MetaTrader. |

## Usage notes
- The strategy expects trades to be opened by external strategies, manual operations or other components. It only manages the stop-loss.
- Ensure the security metadata (`PriceStep`, `Step`, volume) is filled so that the point-to-price conversion matches the broker's tick size. Instruments quoted with fractional pips must expose the proper step.
- Because the stop is recomputed on every candle close it will follow the price even when the market moves against the position. This mirrors the MetaTrader logic where `OrderModify` always uses the latest `Bid`/`Ask` minus/plus the configured distance.
- The created stop orders always replace the previous one to keep the platform in sync with the latest protective level.
