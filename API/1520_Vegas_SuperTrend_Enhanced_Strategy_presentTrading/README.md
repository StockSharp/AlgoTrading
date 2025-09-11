# Vegas SuperTrend Enhanced Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a Vegas channel with an adjusted SuperTrend.
Enters when the SuperTrend flips direction with volatility-based multiplier.

## Details

- **Entry Criteria**: trend changes detected by adjusted SuperTrend
- **Long/Short**: Both (configurable)
- **Exit Criteria**: opposite trend flip
- **Stops**: No
- **Default Values**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, SMA, StandardDeviation
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
