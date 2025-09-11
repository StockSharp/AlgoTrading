# Highs Lows Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading on candle midpoints relative to highest and lowest range.

Buys when the current candle midpoint is below the average of the highest and lowest values and the normalized distance is below LowThreshold. Closes the long position when the midpoint rises above the average and the normalized distance is above HighThreshold.

## Details

- **Entry Criteria**: Midpoint below average and normalized distance below LowThreshold.
- **Long/Short**: Long only.
- **Exit Criteria**: Midpoint above average and normalized distance above HighThreshold.
- **Stops**: No.
- **Default Values**:
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **Filters**:
  - Category: Range
  - Direction: Long
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (240m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
