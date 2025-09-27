# Liquidity Engulfment Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects bullish and bearish engulfing patterns that occur after price touches recent liquidity highs or lows. Trades are filtered by mode and include fixed stop loss and optional take profit defined in pips.

## Details

- **Entry Conditions**:
  - **Long**: Bullish engulfing after lower liquidity touch.
  - **Short**: Bearish engulfing after upper liquidity touch.
- **Exit Conditions**: Opposite signal, stop loss or take profit.
- **Long/Short**: Configurable (both by default).
- **Indicators**: Highest, Lowest.
- **Stops**: `StopLossPips` and optional `TakeProfitPips`.
- **Default Values**:
  - `CandleType` = 1 minute
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
