# TCP Pivot Limit Strategy

## Overview

The TCP Pivot Limit strategy is a conversion of the classic MetaTrader 4 expert **gpfTCPivotLimit.mq4**. The original expert computes daily pivot levels and searches for false breakouts around these levels using hourly candles. Once a breakout fails, the strategy immediately enters a reversal trade targeting the opposite pivot levels. This implementation reproduces the same logic using the StockSharp high-level strategy API.

The strategy operates on hourly candles and keeps only a single open position at any moment. Every new trading day it recalculates the pivot grid from the previous day's high, low, and close values. These levels guide the entry triggers, stop-loss, take-profit, and optional trailing management.

## Trading Logic

1. **Pivot Calculation**
   - At the first candle of each new trading day the strategy aggregates the previous day's high, low, and close to compute classic floor trader pivot levels (Pivot, R1–R3, S1–S3).
   - A log entry is produced whenever new levels are generated so that you can track how the grid evolves.

2. **Entry Conditions**
   - On every finished hourly candle the strategy checks the last two completed candles.
   - A *short* position is opened when the candle two periods ago spiked above a resistance level (or closed at/above it) while opening below it, and the most recent candle closed back under that level. This indicates a failed breakout and expects a reversal lower.
   - A *long* position is opened symmetrically when the market dips below a support level but the following candle closes back above it.
   - Only one position can be active at a time. The order volume is defined by the `OrderVolume` parameter.

3. **Exit Management**
   - Each entry uses the stop-loss and take-profit levels defined by the selected `TargetMode` preset. The presets mirror the `TgtProfit` options from the original expert advisor and combine different pivot levels:
     | Mode | Short Entry | Short Stop | Short Target | Long Entry | Long Stop | Long Target |
     |------|-------------|------------|--------------|------------|-----------|-------------|
     | 1    | R1          | R2         | S1           | S1         | S2        | R1          |
     | 2    | R1          | R2         | S2           | S1         | S2        | R2          |
     | 3    | R2          | R3         | S1           | S2         | S3        | R1          |
     | 4    | R2          | R3         | S2           | S2         | S3        | R2          |
     | 5    | R2          | R3         | S3           | S2         | S3        | R3          |
   - If `IntradayTrading` is enabled, any open position is closed on the 23:00 candle close to avoid holding overnight.
   - An optional trailing stop in points (multiples of the instrument price step) emulates the MetaTrader behaviour. Trailing activates only after the move has advanced by the configured distance and closes the trade when price retraces by the same amount.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Volume used for both buy and sell market orders. |
| `TargetMode` | Integer from 1 to 5 selecting which resistance/support combination is used for entries, stops, and targets. |
| `TrailingPoints` | Trailing stop distance measured in price points. Set to zero to disable trailing. |
| `IntradayTrading` | When `true`, positions are closed at 23:00 to keep trading intraday. |
| `CandleType` | Candle data type. Default is one-hour time frame to match the original expert. |

## Notes

- The strategy expects a continuous stream of hourly candles. Applying it to other time frames changes the behaviour and should be back-tested.
- Stop-loss and take-profit levels are evaluated using candle extremes, so gaps across levels may result in exits at worse prices, just as in the MetaTrader version.
- Trailing management is performed on candle closes and lows/highs, closely matching the original tick-based logic while remaining efficient in the StockSharp environment.
