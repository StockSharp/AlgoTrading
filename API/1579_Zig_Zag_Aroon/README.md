# Zig Zag Aroon Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines simple ZigZag pivot detection with the Aroon indicator. It buys when Aroon Up crosses above Aroon Down and the latest pivot is a high. Short positions are opened when Aroon Down crosses above Aroon Up and the last pivot is a low.

## Details

- **Entry Criteria**: Aroon crossover with matching ZigZag pivot.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Aroon, ZigZag
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
