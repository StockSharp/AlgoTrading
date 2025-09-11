# VWAP Pro V21
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines fast and slow EMA with VWAP and ATR-based risk management. A higher timeframe EMA filter (1h, length 50) confirms the trend. Trades open when price aligns with the trend and close at ATR-based take profit or stop loss levels.

## Details

- **Entry Criteria**: Price above/below fast EMA, VWAP, and trend filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR take profit or stop loss.
- **Stops**: Yes.
- **Default Values**:
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: EMA, VWAP, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
