# Dual RSI Differential
[Русский](README_ru.md) | [中文](README_cn.md)

Dual RSI Differential compares two RSI periods and trades when their difference crosses a threshold. This dual-length approach seeks to capture divergences between short-term and long-term momentum.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`.
  - **Short**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`.
- **Exit Criteria**: Opposite threshold, optional holding period, optional take profit / stop loss.
- **Stops**: Optional take profit and stop loss (`Condition`).
- **Default Values**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **Filters**:
  - Category: Momentum
  - Direction: Long & Short
  - Indicators: RSI
  - Complexity: Basic
  - Risk level: Medium
