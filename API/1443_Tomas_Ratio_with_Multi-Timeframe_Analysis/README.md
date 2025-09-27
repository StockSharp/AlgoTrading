# Tomas Ratio Strategy with Multi-Timeframe Analysis
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy accumulates weighted gains and losses across multiple timeframes to build a Tomas Ratio signal. Trades are opened when the signal strength exceeds a target and closed when weakness dominates.

## Details

- **Entry Criteria**: signal strength exceeds target and price above EMA(720)
- **Long/Short**: Long only
- **Exit Criteria**: close points exceed buy points
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1-hour candles
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Standard Deviation, SMA, EMA
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
