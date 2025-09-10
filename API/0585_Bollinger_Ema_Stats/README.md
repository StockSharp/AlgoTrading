# Bollinger EMA Stats Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses two Bollinger Band sets to define entry and stop zones and an EMA as the exit target.

## Details
- **Entry Criteria**:
  - **Long**: Close < lower Bollinger Band (entry multiplier).
  - **Short**: Close > upper Bollinger Band (entry multiplier).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Profit target at EMA.
  - Stop loss at the wider Bollinger Band.
- **Stops**: Yes.
- **Default Values**:
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: Bollinger Bands, EMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Medium-term
