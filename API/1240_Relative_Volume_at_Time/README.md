# Relative Volume at Time
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that compares volume at a specific time of day to the average volume of recent candles.

## Details

- **Entry Criteria**: Relative volume above threshold at the specified time of day.
- **Long/Short**: Both directions.
- **Exit Criteria**: Relative volume back below 1.
- **Stops**: No.
- **Default Values**:
  - `Period` = 5
  - `Threshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TargetHour` = 9
  - `TargetMinute` = 30
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: SMA, Volume
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
