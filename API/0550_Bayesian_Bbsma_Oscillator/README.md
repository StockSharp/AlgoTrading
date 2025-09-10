# Bayesian BBSMA Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy estimates the probability of the next candle breaking up or down using a Bayesian model built from Bollinger Bands and a simple moving average. Optional confirmation from Bill Williams' Accelerator and Alligator indicators can filter signals. When the probability of an upward break rises above the threshold, a long trade is opened. A high probability of a downward break triggers a short.

## Details

- **Entry Criteria**:
  - Long when the prime or upward probability crosses above `LowerThreshold` (default 15%) and, if enabled, Bill Williams confirmation is bullish.
  - Short when the prime or downward probability crosses above the threshold and, if enabled, Bill Williams confirmation is bearish.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **Filters**:
  - Category: Probabilistic trend following
  - Direction: Both
  - Indicators: Bollinger Bands, SMA, Awesome Oscillator, Accelerator Oscillator, Alligator
  - Stops: No
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
