# ZigAndZag Scalpel Strategy

## Overview
ZigAndZagScalpelStrategy is a StockSharp port of the MetaTrader 4 "ZigAndZag" toolkit (folder 8304).
The original package combines a custom indicator and an expert advisor. Two ZigZag windows are used:

* **KeelOver** – a long lookback swing detector that marks the dominant trend.
* **Slalom** – a short lookback swing detector that defines actionable breakouts.

When the long-term ZigZag flips upward the strategy looks for the next Slalom low and waits for price
to rise a configurable number of points above that pivot. A buy market order is issued once the
breakout distance is met. A symmetrical rule opens a short position when the KeelOver trend turns
down, the Slalom prints a fresh high, and price drops below it. Positions can optionally be closed
as soon as the opposite Slalom pivot is confirmed, mimicking the indicator's limit-arrow removal.

The implementation keeps the daily trade limiter from the expert advisor. Only a configurable number
of trades is allowed per trading day, resetting automatically at midnight (exchange time). This
reproduces the "new day" flag from the original code.

## How it works
1. Subscribe to the primary candle stream defined by `CandleType`.
2. Feed two `ZigZagIndicator` instances:
   * Depth = `KeelOverLength` for the trend detector.
   * Depth = `SlalomLength` for entry signals.
3. Track the most recent KeelOver pivot to determine whether the trend is up (last pivot is a low)
   or down (last pivot is a high).
4. When the Slalom indicator publishes a new pivot, arm a breakout in that direction.
5. Calculate the weighted price `(5×Close + 2×Open + High + Low) / 9`. If price moves more than
   `BreakoutDistancePoints` (converted into price units) away from the pivot while the trend supports
the move, execute a market order.
6. Close existing positions when the global trend flips or the opposite Slalom pivot appears and
   `CloseOnOppositePivot` is enabled.
7. Reset the daily trade counter at every calendar day change.

The parameters `DeviationPoints` and `Backstep` are shared between both ZigZag instances so the
swing structure matches the MetaTrader indicator buffers.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | `15m` | Primary timeframe used to build both ZigZag ladders. |
| `KeelOverLength` | `55` | Long-term ZigZag lookback that defines the trend (original `KeelOver`). |
| `SlalomLength` | `17` | Short-term ZigZag lookback used for entries (original `Slalom`). |
| `DeviationPoints` | `5` | Minimum swing size in points before a new ZigZag pivot is confirmed. |
| `Backstep` | `3` | Required bar distance between consecutive pivots. |
| `BreakoutDistancePoints` | `2` | Distance from a pivot (in points) before firing an order. |
| `MaxTradesPerDay` | `1` | Maximum number of entries per calendar day. Mirrors the original `newday` flag. |
| `CloseOnOppositePivot` | `true` | Close open positions when the Slalom ZigZag produces the opposite swing. |

All point-based parameters are converted to price units using `Security.PriceStep`. If the instrument
has no price step configured, a value of `1` is used to keep the strategy functional during testing.

## Usage notes
* The strategy operates with market orders (`BuyMarket` / `SellMarket`). Attach your own risk rules
  or stop-loss helpers if tighter risk management is required.
* Because both ZigZag indicators share the same candle stream, make sure the chosen `CandleType` is
  supported by your data adapter.
* `MaxTradesPerDay = 1` reproduces the "one trade per day" behaviour. Increase the value if you need
  multiple entries during the same session.
* Set `CloseOnOppositePivot = false` to keep positions open until the global trend reverses instead of
  reacting to every short-term swing.

## Differences vs. the MT4 expert advisor
* The MetaTrader version placed pending limit arrows. The StockSharp port executes breakouts with
  immediate market orders to stay within the high-level API.
* Risk management, lot sizing and partial closes are intentionally omitted. Use StockSharp position
  sizing helpers if you need advanced capital control.
* Indicator buffers 4/5/6 are replaced by direct strategy logic and chart annotations via
  `DrawIndicator` and `DrawOwnTrades`.

## Recommended extensions
* Add stop-loss and take-profit parameters tied to ATR or recent ZigZag swings.
* Overlay the original indicator with `BreakoutDistancePoints = 0` to visualize the raw pivot ladder.
* Combine with a session filter (`IsFormedAndOnlineAndAllowTrading`) to limit trading hours.
