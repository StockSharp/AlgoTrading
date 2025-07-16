# Beta Adjusted Pairs Trading
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Beta Adjusted Pairs Trading strategy uses the Beta alongside volatility filters. It enters trades only when specified conditions align.

Signals require the indicator to surpass a threshold while volatility meets predefined criteria. Positions can be long or short with built-in stops.

Designed for traders who value risk control, the strategy exits as soon as the indicator mean reverts or volatility shifts. Initial setting `Asset2` = (Security.

## Details

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `Asset2` = (Security
  - `Asset2Portfolio` = (Portfolio
  - `BetaAsset1` = 1.0m
  - `BetaAsset2` = 1.0m
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2.0m
  - `StopLoss` = 2.0m
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Beta
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
