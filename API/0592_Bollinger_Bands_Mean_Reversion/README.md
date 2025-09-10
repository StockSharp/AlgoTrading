# Bollinger Bands Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when price closes below the lower Bollinger Band and exits when price closes above the upper band.

## Details

- **Entry Criteria**:
  - **Long**: Close below lower band.
- **Long/Short**: Long only.
- **Exit Criteria**: Close above upper band.
- **Stops**: None.
- **Default Values**:
  - Bollinger Bands length 20.
  - Multiplier 2.
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Bollinger Bands
  - Stops: No
  - Complexity: Simple
  - Timeframe: Short-term
