# CBC Strategy with Trend Confirmation & Separate Stop Loss
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a color bar change (CBC) state to detect flips when price breaks the previous candle's high or low. Entries require trend confirmation via EMA and VWAP and are restricted to a trading session window. Exits apply an ATR-based profit target and use the prior candle's extremums as stop loss levels.

## Details

- **Entry Criteria**: CBC flips, optional strong flip filter, slow EMA relative to VWAP, within trading hours.
- **Long/Short**: Both.
- **Exit Criteria**: ATR multiplier take-profit, previous candle high/low stop loss.
- **Stops**: Yes.
- **Default Values**:
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, VWAP, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
