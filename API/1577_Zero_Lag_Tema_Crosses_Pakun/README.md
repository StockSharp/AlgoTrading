# Zero-Lag TEMA Crosses Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Zero-lag triple EMA crossover system. Positions use recent highs and lows for stops and risk-reward based targets.

## Details

- **Entry Criteria**: Fast TEMA crossing slow TEMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop at recent extreme or target by ratio.
- **Stops**: Yes.
- **Default Values**:
  - `Lookback` = 20
  - `FastPeriod` = 69
  - `SlowPeriod` = 130
  - `RiskReward` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: TEMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
