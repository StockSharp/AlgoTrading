# MFI Slowdown
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy monitors the Money Flow Index (MFI) on a higher timeframe and reacts when it reaches extreme zones. If `SeekSlowdown` is enabled, a signal is confirmed only when the MFI value changes less than one point between two consecutive bars. On an upward signal it closes short positions and optionally opens a new long position; on a downward signal it closes long positions and can open a short one. Risk management is handled by StartProtection.

## Details

- **Entry Criteria**:
  - Upward signal: `MFI >= UpperThreshold` and (no slowdown check or slowdown detected).
  - Downward signal: `MFI <= LowerThreshold` and (no slowdown check or slowdown detected).
- **Long/Short**: Both, depending on parameters.
- **Exit Criteria**:
  - Opposite signal closes the position.
  - Stop-loss and take-profit via `StopLossPercent` and `TakeProfitPercent`.
- **Stops**: Yes, via StartProtection.
- **Default Values**:
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = 6-hour timeframe
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MFI
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: Optional (slowdown check)
  - Risk level: Medium
