# Reflected EMA Difference RED Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reflects the distance between two Hull Moving Averages and tracks a smoothed value. When the smoothed reflection reverses by a specified percentage, it enters long or short positions accordingly.

## Details

- **Entry Criteria**:
  - Long: smoothed reflection rises above its pullback limit.
  - Short: smoothed reflection falls below its pullback limit.
- **Long/Short**: Both
- **Default Values**:
  - `Smoothing Period` = 2
  - `Change Percent` = 0.04
