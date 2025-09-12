# Gemini Trend Following System
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy that buys pullbacks to the 50-day SMA within a strong uptrend confirmed by the 200-day SMA and annual Rate of Change filter.

## Details

- **Entry Criteria**: Price recovers above the 50 SMA after a recent pullback in a confirmed uptrend.
- **Long/Short**: Long only.
- **Exit Criteria**: Death cross of 50 below 200 SMA or catastrophic stop.
- **Stops**: Optional catastrophic stop.
- **Default Values**:
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA, RateOfChange, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
