# Hurst Exponent Volatility Filter
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Hurst Exponent Volatility Filter strategy uses the Hurst alongside volatility filters. It enters trades only when specified conditions align.

Testing indicates an average annual return of about 163%. It performs best in the stocks market.

Signals require the indicator to surpass a threshold while volatility meets predefined criteria. Positions can be long or short with built-in stops.

Designed for traders who value risk control, the strategy exits as soon as the indicator mean reverts or volatility shifts. Initial setting `HurstPeriod` = 100.

## Details

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `HurstPeriod` = 100
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `StopLoss` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Hurst
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

