# Supertrend Advance Pullback Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Supertrend Advance Pullback combines Supertrend with pullback or trend change entries. Optional EMA, RSI, MACD and CCI filters refine signals.

## Details

- **Entry Criteria**: Supertrend pullback or flip with optional EMA, RSI, MACD, CCI filters
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Supertrend, EMA, RSI, MACD, CCI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
