# EMA Moving Away
[Русский](README_ru.md) | [中文](README_cn.md)

EMA Moving Away tracks how far price drifts from an exponential moving average.
When a sequence of candles pushes a set percentage away from the EMA, the
strategy bets on a snap back to the mean.

The setup focuses on the long side: after an extended bearish move drives price
below the EMA by `MovingAwayPercent`, a position is opened. Body‑size and streak
filters can be added to ensure the move is stretched rather than noisy. A
percentage stop‑loss protects capital if the reversion fails.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close below EMA by `MovingAwayPercent` with required streak/size filters.
  - **Short**: not used.
- **Exit Criteria**: Return to EMA or stop‑loss hit.
- **Stops**: Percentage stop based on `StopLossPercent`.
- **Default Values**:
  - `EmaLength` = 55
  - `MovingAwayPercent` = 2.0
  - `StopLossPercent` = 2.0
- **Filters**:
  - Category: Mean reversion
  - Direction: Long only
  - Indicators: EMA
  - Complexity: Moderate
  - Risk level: Medium
