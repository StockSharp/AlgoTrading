# HMA Crossover ATR Curvature
[Русский](README_ru.md) | [中文](README_cn.md)

HMA Crossover ATR Curvature is a trend-following strategy combining a fast and slow Hull Moving Average crossover with a curvature filter. Position size is based on ATR and risk percent, and trades are protected by an ATR-based trailing stop.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Fast HMA crosses above slow HMA and curvature is above threshold.
  - **Short**: Fast HMA crosses below slow HMA and curvature is below negative threshold.
- **Exit Criteria**: ATR trailing stop.
- **Stops**: ATR trailing stop.
- **Default Values**:
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: HMA, ATR
  - Complexity: Low
  - Risk level: Medium
