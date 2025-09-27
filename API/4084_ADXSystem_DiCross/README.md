# ADX System DI Cross Strategy

## Overview
The ADX System strategy is the StockSharp conversion of the MetaTrader 4 expert `ADX_System.mq4`. The original EA compares the
Average Directional Index (ADX) with its +DI and -DI components on the two most recent completed candles. When the +DI line
rises above the ADX value the system wants to be long; when the -DI line rises above the ADX value it wants to be short. The
StockSharp port reproduces this behaviour by storing the indicator values from the previous two finished candles so the logic
mirrors the `iADX(..., shift=1/2)` calls used in the MetaTrader code.

Only one position can be open at any time. The strategy submits market orders for entries and exits, matching the single-ticket
logic of MetaTrader netting accounts. Risk management mirrors the original expert advisor: fixed take-profit and stop-loss
levels are expressed in points relative to the average entry price, and an optional trailing stop can lock in profits once the
position moves favourably.

## Trading logic
1. Subscribe to the configured timeframe (`CandleType`) and process only finished candles to avoid intra-bar decisions.
2. Feed an `AverageDirectionalIndex` indicator with the candle data and wait until the indicator provides its ADX, +DI, and -DI
   values.
3. Cache the indicator readings from the two most recent finished candles so the strategy can reference the "current" and
   "previous" values exactly like the MetaTrader implementation.
4. **Long entry**: if the older ADX (`shift = 2`) is below the more recent ADX (`shift = 1`), the older +DI is below that older
   ADX, and the more recent +DI is above the more recent ADX, send a market buy order.
5. **Short entry**: if the same conditions appear for the -DI component (old -DI below old ADX, new -DI above new ADX), send a
   market sell order.
6. **Long exit**: close the long position when the ADX starts falling and +DI crosses back below it, when the configured
   take-profit or stop-loss is hit, or when the trailing stop is breached.
7. **Short exit**: mirror the long exit logic using -DI together with the risk controls.
8. Update the cached indicator history after every candle so the next signal uses the latest `shift = 1/2` pair.

## Risk management
- `TakeProfitPoints` and `StopLossPoints` describe distances in MetaTrader-style points. They are converted to actual price units
  using `Security.PriceStep` when available; otherwise the raw value is treated as an absolute price delta.
- The trailing stop (`TrailingStopPoints`) activates only after the position gains at least the configured distance from the
  entry price. Once active it moves in the direction of profit and closes the position when price crosses the trailing level.
- All exits (indicator reversal, take-profit, stop-loss, trailing stop) use market orders so the position is flattened
  immediately, mimicking `OrderClose` behaviour from the source EA.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe processed by the strategy. |
| `AdxPeriod` | `int` | `14` | Number of candles used to compute the ADX and the DI components. |
| `TradeVolume` | `decimal` | `1` | Lot size used for every market order. |
| `TakeProfitPoints` | `decimal` | `100` | Take-profit distance in points relative to the entry price. |
| `StopLossPoints` | `decimal` | `30` | Stop-loss distance in points relative to the entry price. |
| `TrailingStopPoints` | `decimal` | `0` | Optional trailing-stop distance in points. Set to zero to disable trailing. |

## Differences from the original MetaTrader expert
- MetaTrader manages individual tickets while StockSharp works with a single net position. The conversion therefore closes the
  current position before issuing a new entry order when the signal flips.
- The original EA relied on `Point` to convert points into price distances. The StockSharp port uses `Security.PriceStep` when it
  is known; otherwise the distance is treated as raw price units, so you may need to adjust the defaults for instruments with
  unconventional price steps.
- MetaTrader applies trailing stops by modifying the existing order. StockSharp closes the position with a market order when the
  trailing stop is violated, which is functionally equivalent but simpler within the netting model.

## Usage tips
- Ensure the strategy volume (`TradeVolume`) aligns with the instrument's lot step. The constructor also assigns this value to
  `Strategy.Volume`, so helper methods use the expected trade size.
- Increase `TakeProfitPoints` and `StopLossPoints` if you trade instruments with larger average ranges or smaller price steps.
- Add the strategy to a chart to visualise the candles, the ADX indicator, and executed trades, which helps verify that signals
  occur exactly when +DI or -DI crosses above the ADX line.

## Indicators
- `AverageDirectionalIndex` (provides ADX together with +DI and -DI components).
