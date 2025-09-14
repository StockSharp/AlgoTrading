# CCI Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Commodity Channel Index (CCI) to detect reversals when the indicator leaves extreme zones. A long position is opened when CCI falls back below the upper level after being above it. A short position is opened when CCI rises above the lower level after being below it. Optional stop loss and take profit levels in points can protect open positions.

## Details

- **Entry Criteria**:
  - **Long**: Previous CCI > `UpperLevel` and current CCI ≤ `UpperLevel`.
  - **Short**: Previous CCI < `LowerLevel` and current CCI ≥ `LowerLevel`.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal closes existing position and opens a new one.
- **Stops**: Optional fixed stop loss and take profit in points.
- **Default Values**:
  - `CCI Period` = 14
  - `Upper Level` = 100
  - `Lower Level` = -100
  - `Stop Loss` = 100 points
  - `Take Profit` = 200 points
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: CCI
  - Stops: Optional
  - Complexity: Simple
  - Timeframe: Any (default 4H)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

