# EMA 10/20/50 Alignment
[Русский](README_ru.md) | [中文](README_cn.md)

This long-only strategy enters when EMA(10) > EMA(20) > EMA(50) and exits when the EMAs align in descending order. Trading is restricted to a configurable date range.

## Details

- **Entry Criteria**: EMA(10) above EMA(20) above EMA(50) within the specified date range.
- **Long/Short**: Long only.
- **Exit Criteria**: EMAs align downward (EMA(10) < EMA(20) < EMA(50)).
- **Stops**: No.
- **Default Values**:
  - `StartTime` = new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `EndTime` = new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
