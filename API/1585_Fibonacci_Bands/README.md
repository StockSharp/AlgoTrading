# Fibonacci Bands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Expands a Keltner Channel by Fibonacci ratios and trades when price breaks the outer band with RSI confirmation.

## Details

- **Entry Criteria**: Price crosses `fbUpper3` with RSI above 60 for long; crosses `fbLower3` with RSI below 40 for short.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing back over the moving average.
- **Stops**: No.
- **Default Values**:
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 minutes
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: MA, ATR, RSI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
