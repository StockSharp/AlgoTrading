# Gold Scalping Strategy with Precise Entries
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping strategy for gold using EMA trend filter, RSI range and engulfing patterns.

## Details

- **Entry Criteria**: EMA trend filter with RSI between 45 and 55 plus bullish/bearish engulfing near EMA50.
- **Long/Short**: Both directions.
- **Exit Criteria**: Take profit or stop loss.
- **Stops**: ATR-based stop loss and fixed pip target.
- **Default Values**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: EMA, RSI, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
