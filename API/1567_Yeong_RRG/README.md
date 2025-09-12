# Yeong RRG
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on normalized relative strength and momentum ratio (RRG).

The strategy enters long when both JDK RS and JDK RoC are above 100 and exits when both fall below 100.

## Details

- **Entry Criteria**: JDK RS and JDK RoC above 100.
- **Long/Short**: Long only.
- **Exit Criteria**: JDK RS and JDK RoC below 100.
- **Stops**: No.
- **Default Values**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Relative Strength
  - Direction: Long
  - Indicators: SMA, ROC, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

