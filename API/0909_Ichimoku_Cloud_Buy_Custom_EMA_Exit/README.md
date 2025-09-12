# Ichimoku Cloud Buy Custom EMA Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implementation of strategy - Ichimoku Cloud Buy with custom EMA exit and volume filter. The strategy buys when price is above the cloud and volume exceeds its average. Optionally it requires price to stay above the EMA. The position is closed once price falls below the EMA or when the stop-loss is hit.

## Details

- **Entry Criteria**:
  - Long: `Price > Cloud && Volume > AvgVolume && (Price > EMA if enabled)`
- **Long/Short**: Long only
- **Exit Criteria**:
  - `Price < EMA`
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `EmaLength` = 44
  - `VolumeAvgPeriod` = 10
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend-following
  - Direction: Long
  - Indicators: Ichimoku Cloud, EMA, Volume
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
