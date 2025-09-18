# Nextbar Strategy

## Overview
The **Nextbar Strategy** is a direct translation of the MetaTrader 4 expert advisor `nextbar.mq4`. The original EA evaluates the distance between the last completed candle and a candle that is several bars older. When price travels far enough in one direction it either follows the momentum or trades against it, depending on the configured direction flag. Positions are then protected with symmetric take-profit/stop-loss levels and are force-closed after a fixed number of bars.

This StockSharp version keeps the same behaviour while using the high-level strategy API. It processes completed candles only, ensuring that all calculations match the bar-on-close logic of the MT4 script.

## Original MQL logic
* **Momentum distance** – compare `Close[1]` with `Close[bars2check+1]`. If the difference is at least `minbar * Point`, treat it as a valid signal.
* **Direction flag** – the MQL input `direction` equals `1` for trend-following (buy after a rally, sell after a drop) and `2` for contrarian trading (buy after a drop, sell after a rally).
* **Entry constraint** – only one order can be open at a time. A new trade is sent at the start of the bar following the signal.
* **Exit rules** – close a long if the last close hits the profit distance above the entry or the loss distance below it; the inverse applies for shorts. If neither level is reached, close the trade after `bars2hold` completed candles.

## StockSharp implementation highlights
* Uses `SubscribeCandles()` and `Bind` to receive completed candles on the configured timeframe.
* Stores a short rolling history of close prices to reference the candle that matches the MQL `bars2check + 1` offset.
* Converts all point-based parameters with `Security.PriceStep`, mimicking the MetaTrader `Point` constant.
* Places market orders with the strategy `Volume` and supports either momentum-following or contrarian entries via the `Direction` parameter.
* Implements profit, loss, and holding-period exits exactly once per finished candle to stay aligned with the original workflow.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `CandleType` | Timeframe used for signal evaluation. | 1-hour time frame | Attach the strategy to a security that can provide this candle type. |
| `BarsToCheck` | Number of completed candles between the reference close and the latest close. | 8 | Matches `bars2check` from the EA. |
| `BarsToHold` | Maximum number of completed candles to keep a position open. | 10 | Matches `bars2hold`. The position is closed on the bar where the counter reaches this number. |
| `MinMovePoints` | Minimum distance (in MetaTrader points) between the two compared closes. | 77 | Corresponds to `minbar`. Converted using `Security.PriceStep`. |
| `TakeProfitPoints` | Profit target distance in MetaTrader points. | 115 | Equivalent to the `profit` input. Set to zero to disable if desired. |
| `StopLossPoints` | Stop-loss distance in MetaTrader points. | 115 | Equivalent to the `loss` input. Set to zero to disable if desired. |
| `Direction` | Trading mode: `Follow` (trend) or `Reverse` (contrarian). | `Follow` | Mirrors the `direction` input (`1` = follow, `2` = reverse). |
| `Volume` | Trade volume used for market orders. | Strategy volume | Configure through the standard `Strategy.Volume` property. |

## Trading workflow
1. Wait for a finished candle and cache its close price.
2. Fetch the close from `BarsToCheck` candles ago and compute the difference.
3. If the absolute move is below `MinMovePoints * PriceStep`, do nothing.
4. Otherwise:
   * In **Follow** mode, buy if price rose, sell if price fell.
   * In **Reverse** mode, buy if price fell, sell if price rose.
5. On every subsequent finished candle while the position is open:
   * Close longs when the close is `TakeProfitPoints` above or `StopLossPoints` below the stored entry price.
   * Close shorts when the close is `TakeProfitPoints` below or `StopLossPoints` above the entry.
   * Force-close once `BarsToHold` candles have elapsed since entry.

## Usage notes
* The conversion from points to absolute price requires `Security.PriceStep`. Provide the correct instrument metadata (price step, step price, volume rules) before running the strategy.
* The strategy does not manage multiple simultaneous positions; ensure `Volume` corresponds to the size you expect for a single MT4 order.
* Because decisions are evaluated on completed candles only, the strategy should be run with historical and real-time data that deliver finished bars.
