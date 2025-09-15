# ADX DMI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses the Directional Movement Index (DMI) to trade crossovers between the +DI and -DI lines. When -DI moves above +DI and then drops below it, the strategy opens a long position. When +DI rises above -DI and then falls back below, it opens a short position. Opposite signals can optionally close existing positions.

## Details

- **Entry Criteria**:
  - **Long**: -DI was above +DI on the previous bar and crosses below it on the latest bar.
  - **Short**: +DI was above -DI on the previous bar and crosses below it on the latest bar.
- **Exit Criteria**:
  - Reverse crossover if corresponding close option is enabled.
- **Indicators**:
  - Directional Index (period 14 by default)
- **Stops**: none by default.
- **Default Values**:
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **Filters**:
  - Works on any timeframe
  - Indicators: DMI
  - Stops: optional via external risk management
  - Complexity: basic
