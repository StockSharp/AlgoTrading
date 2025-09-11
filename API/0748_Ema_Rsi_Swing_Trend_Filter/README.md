# EMA RSI Swing Trend Filter
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the crossover of EMA20 and EMA50 in the direction of an EMA200 trend filter.
An optional RSI filter limits long entries when RSI is overbought and shorts when it is oversold.

## Details

- **Entry Criteria**: EMA20 crosses EMA50 with price relative to EMA200 and optional RSI filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Optional exit on opposite EMA cross.
- **Stops**: No.
- **Default Values**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
