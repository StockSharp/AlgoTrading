# ColorXADX
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the crossing of +DI and -DI lines confirmed by ADX strength.

The system monitors the Directional Movement indicators. When +DI crosses above -DI with the Average Directional Index exceeding a
set threshold, it enters a long position and exits any existing short. Conversely, a bearish cross (-DI above +DI) with strong ADX
opens a short and closes longs. Stop-loss and take-profit levels are applied to manage risk.

## Details

- **Entry Criteria**: +DI/-DI cross with ADX above threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop levels.
- **Stops**: Yes, fixed stop-loss and take-profit.
- **Default Values**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 30m
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, DMI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Swing (4h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
