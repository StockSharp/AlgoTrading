# Trade Panel Autopilot Strategy

## Overview

This strategy reproduces the core logic of the original MQL4 "trade panel with autopilot" expert. It aggregates price direction across multiple timeframes and opens or closes a single position according to the dominant market sentiment.

The strategy monitors the latest two candles on eight different timeframes (M1, M5, M15, M30, H1, H4, D1, W1). For each timeframe it compares several price components between the two most recent candles:

- Open
- High
- Low
- (High + Low) / 2
- Close
- (High + Low + Close) / 3
- (High + Low + Close + Close) / 4

Each comparison contributes to either a **buy** or **sell** score. Scores from all timeframes are summed and converted to percentages. When the buy or sell percentage crosses a configured threshold the strategy enters a position. The existing position is closed if the opposite percentage drops below the closing threshold.

## Parameters

- `Autopilot` — enables or disables automatic trading.
- `OpenThreshold` — percentage level required to open a new position. Default: 85.
- `CloseThreshold` — percentage level to close an existing position. Default: 55.
- `LotFixed` — fixed order volume when `UseFixedLot` is enabled.
- `LotPercent` — volume as a percentage of portfolio value when `UseFixedLot` is disabled.
- `UseFixedLot` — toggles between fixed and percentage volume.
- `UseStopLoss` — starts position protection when enabled.

## Trading Logic

1. Subscribe to candles on all configured timeframes.
2. Calculate buy/sell scores for each new finished candle.
3. Sum scores across timeframes and compute buy/sell percentages.
4. If `Autopilot` is disabled the strategy only tracks scores.
5. If no position is open and the buy percentage exceeds `OpenThreshold`, enter a long position. If the sell percentage exceeds the threshold, enter a short position.
6. If a long position exists and the buy percentage falls below `CloseThreshold`, exit the position. The same logic applies for short positions using the sell percentage.

## Notes

- The strategy maintains at most one open position at a time.
- Optional stop-loss management is activated via `StartProtection()` when `UseStopLoss` is true.
