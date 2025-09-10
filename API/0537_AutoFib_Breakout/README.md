# AutoFib Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy draws a dynamic Fibonacci extension from recent swing high and low and goes long when price breaks above the 1.618 level during an uptrend defined by the 200 EMA. Risk is managed using ATR-based stop and target.

## Details

- **Entry Criteria**: Close above 1.618 Fibonacci extension and above EMA200.
- **Long/Short**: Long only.
- **Exit Criteria**: ATR-based stop-loss or 3×ATR take profit.
- **Stops**: Yes, based on ATR.
- **Default Values**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `FibLevel` = 1.618
  - `PivotPeriod` = 10
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: EMA, ATR, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
