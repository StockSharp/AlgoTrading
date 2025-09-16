# Analyze History Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that scans historical bar data and logs time gaps larger than a specified number of bars. Weekend gaps are ignored while holiday gaps are reported.

## Details

- **Entry Criteria**: None
- **Long/Short**: None
- **Exit Criteria**: None
- **Stops**: No
- **Default Values**:
  - `MinGapInBars` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Analysis
  - Direction: None
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
