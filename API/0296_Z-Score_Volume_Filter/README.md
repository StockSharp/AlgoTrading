# Z-Score Volume Filter
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Z-Score Volume Filter strategy uses the Z-Score alongside volatility filters. It enters trades only when specified conditions align.

Signals require the indicator to surpass a threshold while volatility meets predefined criteria. Positions can be long or short with built-in stops.

Designed for traders who value risk control, the strategy exits as soon as the indicator mean reverts or volatility shifts. Initial setting `LookbackPeriod` = 20.

## Details

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Z-Score
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
