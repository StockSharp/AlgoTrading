# Fisher Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Fisher Transform indicator to enter long positions when the indicator crosses above its previous value while below 1. Positions are closed when the indicator crosses back below its previous value while above 1.

## Details

- **Entry Criteria**:
  - **Long**: `Fisher crosses above previous Fisher` && `Fisher < 1`
- **Long/Short**: Long only
- **Exit Criteria**:
  - `Fisher crosses below previous Fisher` && `Fisher > 1`
- **Stops**: No
- **Default Values**:
  - `Fisher Length` = 9
- **Filters**:
  - Category: Trend following
  - Direction: Long only
  - Indicators: Fisher Transform
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
