# Flash Strategy Minervini Qualifier Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines EMA crossover, SuperTrend and momentum RSI with Minervini stage analysis to qualify trades.

## Details

- **Entry Criteria**: EMA above trailing line, SuperTrend trend and momentum RSI above threshold with Minervini stage filter
- **Long/Short**: Both
- **Exit Criteria**: opposite trailing or SuperTrend flip
- **Stops**: No
- **Default Values**:
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, SuperTrend, RSI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
