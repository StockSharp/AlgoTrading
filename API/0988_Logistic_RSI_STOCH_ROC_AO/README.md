# Logistic RSI STOCH ROC AO
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy applies a logistic map to a selected indicator (AO, ROC, RSI, Stochastic) and trades when the signed standard deviation crosses zero.

## Details

- **Entry Criteria**: Signed standard deviation crosses above zero.
- **Long/Short**: Both directions.
- **Exit Criteria**: Signed standard deviation crosses below zero.
- **Stops**: None.
- **Default Values**:
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
