# Correlation Arrays Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes a rolling correlation matrix for up to six securities. It logs correlation levels using configurable thresholds to help evaluate relationships between assets. The strategy is analysis-only and does not place trades.

## Details
- **Entry Criteria**: None (analysis only)
- **Long/Short**: None
- **Exit Criteria**: None
- **Stops**: None
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **Filters**:
  - Category: Statistical analysis
  - Direction: None
  - Indicators: Correlation
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
