# MACD Enhanced Strategy MTF with Stop Loss
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-timeframe strategy using MACD-based scoring and an ATR-derived trailing stop line.

## Details

- **Entry Criteria**: MACD score turns positive or negative.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or trailing stop line break.
- **Stops**: ATR trailing stop.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
