# Medico Action Zone Self Adjust TF Version 2
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy with higher timeframe confirmation. A position opens when the fast EMA crosses the slow EMA and the higher timeframe close is above the fast EMA. The position reverses on the opposite signal.

## Details

- **Entry Criteria**: Fast EMA crosses above slow EMA with higher timeframe close above fast EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover with confirmation.
- **Stops**: None.
- **Default Values**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
