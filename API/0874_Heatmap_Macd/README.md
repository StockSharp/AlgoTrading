# Heatmap MACD
[Русский](README_ru.md) | [中文](README_cn.md)

This system uses a heatmap of MACD histograms from five timeframes. When all histograms switch above or below zero, it enters in the corresponding direction and exits once the alignment breaks or risk limits trigger.

## Details

- **Entry Criteria**: All MACD histograms above/below zero.
- **Long/Short**: Both directions.
- **Exit Criteria**: Histogram alignment breaks or stops.
- **Stops**: Yes.
- **Default Values**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
