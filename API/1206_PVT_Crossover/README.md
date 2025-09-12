# PVT Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the crossover of the Price Volume Trend (PVT) indicator and its exponential moving average (EMA). A long position is opened when PVT crosses above its EMA, and a short position is opened when it crosses below.

## Details

- **Entry Criteria**:
  - **Long**: PVT crosses above its EMA.
  - **Short**: PVT crosses below its EMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Reverse position on opposite signal.
- **Stops**: No.
- **Default Values**:
  - `EmaLength` = 20.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: PVT, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
