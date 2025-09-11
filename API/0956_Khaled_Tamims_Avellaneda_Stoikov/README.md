# Khaled Tamim's Avellaneda-Stoikov Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the Avellaneda-Stoikov market making model. The strategy computes bid and ask quotes from the last two closes and places market orders when price deviates beyond configurable margins.

## Details

- **Entry Criteria**:
  - **Long**: `close < bidQuote - M`
  - **Short**: `close > askQuote + M`
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **Filters**:
  - Category: Market making
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
