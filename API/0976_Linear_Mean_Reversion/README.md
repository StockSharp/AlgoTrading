# Linear Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Linear Mean Reversion Strategy uses the z-score of price relative to a moving average to trade mean reversion with a fixed stop loss in points.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: z-score < -EntryThreshold.
  - **Short**: z-score > EntryThreshold.
- **Exit Criteria**: z-score returns toward zero (z-score > -ExitThreshold for longs, z-score < ExitThreshold for shorts).
- **Stops**: Fixed stop loss in points.
- **Default Values**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **Filters**:
  - Category: Mean reversion
  - Direction: Long & Short
  - Indicators: SMA, StandardDeviation
  - Stops: Yes
  - Complexity: Low
  - Risk level: Medium
