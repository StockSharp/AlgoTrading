# Trend Signals with TP & SL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses an ATR-based channel to determine trend direction. A new uptrend starts when price breaks above the upper band, triggering a long entry. A downtrend starts when price falls below the lower band, triggering a short entry. Each trade places stop-loss and take-profit orders using ATR multipliers.

## Details

- **Entry Criteria**:
  - **Long**: Trend flips upward.
  - **Short**: Trend flips downward.
- **Exits**: Stop-loss at `entry ∓ ATR * SL` and take-profit at `entry ± ATR * TP`.
- **Stops**: Yes, both stop-loss and take-profit.
- **Default Values**:
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
