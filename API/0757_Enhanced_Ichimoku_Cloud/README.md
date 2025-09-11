# Enhanced Ichimoku Cloud Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only Ichimoku strategy with a 171-day EMA filter. The strategy buys when span A is above span B, price breaks the high from 25 bars ago, Tenkan-sen is above Kijun-sen and the close is above the EMA. Position is closed when Tenkan falls below Kijun.

## Details

- **Entry Criteria**: spanA > spanB, close > high[25], Tenkan > Kijun, close > EMA.
- **Long/Short**: Long only.
- **Exit Criteria**: Tenkan < Kijun.
- **Stops**: No.
- **Default Values**:
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Ichimoku, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
