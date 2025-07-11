# Upthrust Reversal Strategy

Upthrust Reversal is the bearish companion to the spring and occurs when price briefly breaks above resistance but quickly falls back.
The move flushes out late buyers before reversing lower.

This strategy sells short once price drops back under the breakout level, expecting supply to overwhelm demand.

A stop just above the upthrust high manages risk and positions exit if price recovers above that level.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Wyckoff
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
