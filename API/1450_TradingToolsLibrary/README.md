# Trading Tools Library Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simple SMA crossover strategy with RSI filter and entry cooldown.

## Details
- **Entry Criteria**:
  - **Long**: fast SMA crosses above slow SMA and RSI below `RsiUpper`
  - **Short**: fast SMA crosses below slow SMA and RSI above `RsiLower`
- **Long/Short**: Both
- **Exit Criteria**:
  - Reverse signal
- **Stops**: None
- **Default Values**:
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
