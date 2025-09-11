# Gartley 222 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades long when a bullish Gartley 222 harmonic pattern forms.
The pattern is detected using pivot highs and lows validated by Fibonacci ratios.

A long position is opened `PivotLength` bars after confirmation when price closes above point C.
Protection closes the position at a Fibonacci extension target or at a fixed percent stop loss.

## Details

- **Entry Criteria**:
  - Bullish Gartley 222 pattern confirmed
  - Entry delayed by `PivotLength` bars
- **Long/Short**: Long only
- **Exit Criteria**:
  - Stop loss or take profit
- **Stops**:
  - `Stop Loss %` below entry
  - `TP Fib Extension` above entry
- **Default Values**:
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Pivot points, Fibonacci
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
