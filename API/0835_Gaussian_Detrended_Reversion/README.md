# Gaussian Detrended Reversion
[Русский](README_ru.md) | [中文](README_cn.md)

Gaussian Detrended Reversion is a mean-reversion strategy that uses a detrended price oscillator smoothed with an Arnaud Legoux Moving Average (ALMA). Long positions open when the smoothed oscillator crosses above its lagged version while below zero; shorts open on downward crosses above zero. Positions exit on opposite crosses or when the oscillator crosses the zero line.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: ALMA-smoothed DPO crosses above its lag line and is below zero.
  - **Short**: ALMA-smoothed DPO crosses below its lag line and is above zero.
- **Exit Criteria**: Opposite lag cross or zero-line cross.
- **Stops**: None.
- **Default Values**:
  - `PriceLength` = 52
  - `SmoothingLength` = 52
  - `LagLength` = 26
- **Filters**:
  - Category: Mean reversion
  - Direction: Long & Short
  - Indicators: EMA, ALMA
  - Complexity: Low
  - Risk level: Medium
