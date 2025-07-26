# Overnight Gap Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Overnight Gap plays the open when price gaps significantly from the prior close due to news or after-hours activity.
Large gaps often retrace partially as traders digest the move.

Testing indicates an average annual return of about 124%. It performs best in the forex market.

The strategy fades excessive gaps, entering in the opposite direction shortly after the open and closing before the session ends.

Stops are based on a percentage beyond the gap extremes to manage risk if the move continues.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Gap
  - Direction: Both
  - Indicators: Gap
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

