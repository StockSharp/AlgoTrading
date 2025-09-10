# BullBear Volume Percentile TP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bull/Bear Power normalized by a Z-Score.
Long positions are opened when the Z-Score crosses above the threshold,
while short positions are opened when it crosses below the negative threshold.
Take profit levels are based on ATR multipliers adjusted by volume and price percentiles.

## Details

- **Entry Criteria:**
  - **Long**: Z-Score crosses above `ZThreshold`.
  - **Short**: Z-Score crosses below `-ZThreshold`.
- **Long/Short**: Both.
- **Exit Criteria**: Z-Score crosses back through zero or take-profit levels.
- **Stops**: Take profit via ATR multipliers.
- **Default Values:**
  - EMA length 21, Z-Score length 252, threshold 1.618.
  - ATR period 20, multipliers 1.618 / 2.382 / 3.618.
  - Volume MA period 100, percentile period 100.
- **Filters:**
  - Category: Momentum
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
