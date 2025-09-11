# Multi-Timeframe Parabolic SAR
[Русский](README_ru.md) | [中文](README_cn.md)

Combines Parabolic SAR signals from multiple timeframes. Long trades trigger when price stays above the SAR levels selected by the parameters. Short trades appear when price falls below the chosen SARs. Optional stop loss, trailing stop and take profit are available.

## Details

- **Entry Criteria**:
  - **Long**: Price above SAR according to `LongSource` setting.
  - **Short**: Price below SAR according to `ShortSource` setting.
- **Exit Criteria**:
  - Opposite SAR crossover or protection triggers.
- **Indicators**:
  - Parabolic SAR on current timeframe
  - Optional Parabolic SAR on higher and lower timeframes
- **Stops**: Optional stop loss, trailing stop, take profit via StartProtection.
- **Default Values**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **Filters**:
  - Timeframe: main 5m, higher 1d, lower 1m
  - Indicators: Parabolic SAR
  - Stops: optional
  - Complexity: Moderate
