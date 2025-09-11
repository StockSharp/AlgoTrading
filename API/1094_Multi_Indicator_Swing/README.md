# Multi Indicator Swing
[Русский](README_ru.md) | [中文](README_cn.md)

Swing strategy combining Parabolic SAR, SuperTrend, ADX and volume delta confirmation.

## Details

- **Entry Criteria**: All enabled indicators agree.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or hitting stop-loss/take-profit.
- **Stops**: Optional percentage based levels.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: PSAR, SuperTrend, ADX, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (2m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
