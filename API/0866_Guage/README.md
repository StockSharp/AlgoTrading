# Gauge Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy mirrors the TradingView gauge library by measuring price position
between a user defined minimum and maximum. When the percentage crosses the
upper or lower thresholds it enters trades in the corresponding direction.

## Details

- **Entry Criteria**:
  - **Long**: gauge ratio above upper threshold.
  - **Short**: gauge ratio below lower threshold.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal generates an exit.
- **Stops**: None.
- **Default Values**:
  - Min value = 0, Max value = 100.
  - Upper threshold = 75%, Lower threshold = 25%.
- **Filters**:
  - Category: Range / Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
