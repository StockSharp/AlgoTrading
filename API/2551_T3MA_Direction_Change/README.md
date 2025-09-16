# T3 MA Direction Change Strategy

## Overview
This strategy reproduces the behavior of the original **T3MA(barabashkakvn's edition)** expert advisor. The Expert Advisor relies on the "T3MA-ALARM" indicator that applies exponential smoothing twice and raises a signal when the smoothed line changes direction. The StockSharp port keeps the same concept: it creates a double-smoothed exponential moving average (EMA of EMA) and trades whenever the slope of that curve flips from falling to rising or vice versa.

The strategy operates on finished candles only. Signals can be delayed by a configurable number of bars to mimic the original `InpBarNumber` option (default delay is one bar). Orders are placed using market execution so that the portfolio switches between long and short exposure without accumulating multiple concurrent hedged positions.

## Trading Rules
1. Subscribe to the configured candle series and calculate an EMA of the close prices. Run a second EMA on top of the first EMA output, producing the smoothed series used by the indicator.
2. Compare the current value of the smoothed series (optionally shifted forward by `EMA Shift`) with the previous value. The slope is considered bullish when the series increases and bearish when it decreases.
3. When the slope flips from bearish to bullish, enqueue a **buy** signal. When the slope flips from bullish to bearish, enqueue a **sell** signal. Neutral candles push a zero signal into the queue so that the delay counter remains accurate.
4. After the configured `Signal Delay` number of completed candles passes, execute the queued signal. A delayed buy closes any open short position and enters long with the base `Trade Volume`. Likewise, a delayed sell closes a long position and enters short.
5. Protective stop-loss and take-profit orders are initialized via `StartProtection`. Both distances are expressed in price steps so they automatically adapt to the selected instrument tick size.

## Parameters
| Name | Description |
| --- | --- |
| `EMA Length` | Length of the EMA used for both smoothing passes. This matches the `MAPeriod` input in the MetaTrader implementation. |
| `EMA Shift` | Number of bars by which the smoothed EMA is shifted before comparing slopes. Equivalent to the indicator's `MAShift`. |
| `Signal Delay` | Number of completed candles to wait before executing a signal. This mirrors `InpBarNumber` so a value of 1 trades the previous bar's signal. |
| `Stop Loss (steps)` | Stop-loss distance measured in price steps. Set to zero to disable the stop-loss protection. |
| `Take Profit (steps)` | Take-profit distance measured in price steps. Set to zero to disable the take-profit protection. |
| `Trade Volume` | Base order size used for new entries. When reversing a position the strategy adds the current absolute position size to this value. |
| `Candle Type` | Candle data type used for calculations (default: 5-minute time frame). |

## Risk Management
* `StartProtection` automatically registers stop-loss and take-profit levels when the strategy starts. Both levels follow the instrument's tick size and remain active for the entire life of the strategy.
* Position flips are executed using market orders. When the signal direction matches the current exposure, no additional trades are issued, preventing unwanted pyramiding.
* Logging statements are emitted on every trade to keep track of the reason and the reference price taken from the source candle.

## Differences from the MQL5 Version
* The MetaTrader 5 expert required a hedging account and could accumulate multiple positions. The StockSharp version keeps a single net position and reverses it when the opposite signal fires.
* Signal processing is candle-based and happens once per finished candle instead of on every tick, which is more natural within StockSharp's high-level API.
* Stop-loss and take-profit management is handled via `StartProtection` instead of manually submitting SL/TP prices with each order.
* English comments, structured parameters, and chart helpers are added for better readability in the StockSharp environment.

## Usage Notes
1. Attach the strategy to the desired security and ensure that the candle type matches the timeframe that was used when optimizing the original Expert Advisor.
2. Adjust `EMA Length` and the risk parameters to fit the instrument volatility. Higher delays (`Signal Delay`) slow down responses and may filter noise.
3. Because the strategy works with price steps, verify that the security's `PriceStep` property is configured correctly so the protective orders are placed at meaningful distances.
