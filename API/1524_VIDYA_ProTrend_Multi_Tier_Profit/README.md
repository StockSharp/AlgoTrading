# VIDYA ProTrend Multi-Tier Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy using fast and slow VIDYA averages with a Bollinger Band filter.
Optional multi-step take profit orders are placed using ATR multiples and percentage targets.

## Details

- **Entry Criteria**: fast VIDYA above slow VIDYA with price outside Bollinger filter
- **Long/Short**: Both
- **Exit Criteria**: opposite slope or cross
- **Stops**: No
- **Default Values**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: VIDYA, Bollinger Bands, ATR
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
