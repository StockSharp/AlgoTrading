# Zig Dan Zag Ultimate Investment Long Term
[Русский](README_ru.md) | [中文](README_cn.md)

Long-term investment strategy that combines ZigZag pivots with a slow SMA trend filter. A position is opened when a new ZigZag low forms above the SMA, while exits occur on opposite pivots below the SMA.

## Details
- **Entry Criteria**: New ZigZag low above SMA.
- **Long/Short**: Long only.
- **Exit Criteria**: ZigZag high below SMA.
- **Stops**: No.
- **Default Values**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Highest, Lowest, SimpleMovingAverage
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Long term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
