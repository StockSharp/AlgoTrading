# Flat 001a Range Strategy

## Overview
Flat 001a is a scalping system designed for the EURUSD hourly chart. It scans the most recent three hourly candles and measures the distance between the highest high and the lowest low. When the range of this three-candle window stays within a configurable number of points, the strategy anticipates that price will remain trapped inside the flat. It then looks to fade short-term excursions into the upper or lower quarter of the channel and immediately attaches protective orders.

The original MQL4 expert adviser traded only EURUSD on H1 and rejected trading if the symbol or timeframe was incorrect. This port keeps the same defaults (EURUSD, 60-minute candles) and reproduces all entry, stop-loss, take-profit, and trailing-stop calculations in StockSharp.

## Indicators and data
- `Highest` and `Lowest` indicators (period = 3) track the top and bottom of the last three finished candles.
- A time-frame parameter defaults to 60-minute candles to mirror the H1 requirement of the source code.
- No additional oscillators or smoothing filters are used, so the strategy reacts solely to raw price extremes.

## Entry logic
1. Wait for the subscription candle to close. Only finished candles are processed.
2. Verify that the current security code matches the configured code (default: `EURUSD`). If it does not, the strategy remains idle.
3. Evaluate the optional trading window. By default, entries are allowed during the two hours starting at midnight platform time (hours 0 and 1). The filter can be disabled.
4. Compute the three-candle range `range = highest - lowest` and translate it to points via the instrument `PriceStep`.
5. Continue only if the number of points lies between `DiffMinPoints` and `DiffMaxPoints`.
6. If the closing price sits inside the lowest quarter of the range and no position is open, enter a long trade.
7. If the closing price sits inside the highest quarter of the range and no position is open, enter a short trade.

## Order management
- **Initial stop-loss**
  - Long trades: `lowest - range / 3`.
  - Short trades: `highest + range / 3`.
- **Take-profit**
  - Long trades: entry price + `TakeProfitPoints * PriceStep`.
  - Short trades: entry price − `TakeProfitPoints * PriceStep`.
- **Trailing-stop**
  - Once the unrealized profit exceeds `TrailingStopPoints * PriceStep`, the stop-loss is trailed candle by candle.
  - Long trades move the stop to `closePrice - TrailingDistance` if that is higher than the current stop.
  - Short trades move the stop to `closePrice + TrailingDistance` if that is lower than the current stop.
- All exits are executed with market orders. The strategy closes the full position when either the stop-loss or take-profit level is touched by the subsequent candle.

## Parameters
| Group | Name | Description | Default |
| --- | --- | --- | --- |
| General | `CandleType` | Candle type used for calculations. Should be set to a 60-minute timeframe to match the original system. | `TimeFrame(60m)` |
| General | `SecurityCode` | Expected security code. Leave empty to trade any instrument. | `EURUSD` |
| Range Filter | `DiffMinPoints` | Minimum three-candle range in points required to trade. | `18` |
| Range Filter | `DiffMaxPoints` | Maximum three-candle range in points allowed to trade. | `28` |
| Trading Window | `EnableTimeFilter` | Enables or disables the hour filter. | `true` |
| Trading Window | `OpenHour` | Starting hour (0–23) for the trading window. The strategy also allows the immediate next hour. | `0` |
| Risk Management | `TakeProfitPoints` | Take-profit distance expressed in points. Set to zero to disable. | `8` |
| Risk Management | `TrailingStopPoints` | Trailing-stop distance expressed in points. Set to zero to disable trailing. | `6` |

## Practical notes
- The StockSharp `Strategy.Volume` property controls the order size. Adjust it according to your broker contract size.
- Ensure that the selected instrument exposes a valid `PriceStep`. If `PriceStep` is missing, the strategy falls back to `1` and logs a warning.
- The MQL4 expert adviser offered optional money management by scaling lots according to account balance. StockSharp’s sample keeps the position size constant; you can script your own volume management if required.
- Always test the strategy in simulation before running it live. The trailing logic assumes that the broker will fill protective orders when candle extremes cross the level; in fast markets, slippage may increase realized risk.
