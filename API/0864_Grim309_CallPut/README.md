# GRIM309 CallPut Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

GRIM309 CallPut Strategy trades based on multiple EMA trend alignment with a warning system. Long positions enter when short-term EMAs confirm an uptrend and EMA5 is rising above EMA10. Short positions enter on the opposite conditions. A cooldown period prevents immediate re-entry after a close. An additional warning triggers early exits when the EMA5-EMA10 spread contracts rapidly.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: EMA10 above EMA20, price above EMA50, EMA5 rising above EMA10, no position and cooldown satisfied.
  - **Short**: EMA10 below EMA20, price below EMA50, EMA5 falling below EMA10, no position and cooldown satisfied.
- **Exit Criteria**: Price crossing EMA15 or warning signal.
- **Stops**: None.
- **Default Values**:
  - `Ema5Length` = 5
  - `Ema10Length` = 10
  - `Ema15Length` = 15
  - `Ema20Length` = 20
  - `Ema50Length` = 50
  - `Ema200Length` = 200
  - `CooldownBars` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: EMA
  - Complexity: Medium
  - Risk level: Medium
