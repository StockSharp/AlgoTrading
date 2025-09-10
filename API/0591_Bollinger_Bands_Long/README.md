# Bollinger Bands Long Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when the price closes below the lower Bollinger Band and the RSI is oversold. It exits the long position once price returns to the middle band.

## Details

- **Entry Criteria**:
  - Price closes below the lower Bollinger Band.
  - RSI below the oversold level.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Price closes at or above the middle Bollinger Band.
- **Stops**: No.
- **Default Values**:
  - `BbLength` = 10
  - `BbDeviation` = 2
  - `RsiLength` = 14
  - `RsiOversold` = 30
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Bollinger Bands, RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
