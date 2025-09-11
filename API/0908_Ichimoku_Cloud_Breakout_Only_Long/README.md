# Ichimoku Cloud Breakout Only Long
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy enters long positions when price breaks above the Ichimoku cloud and exits when price falls back below it. Only long trades are taken.

## Details

- **Entry Criteria**:
  - Long: `Close` crosses above `max(SenkouA, SenkouB)`
- **Long/Short**: Long only
- **Exit Criteria**:
  - `Close` crosses below `min(SenkouA, SenkouB)`
- **Stops**: None
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Ichimoku
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
