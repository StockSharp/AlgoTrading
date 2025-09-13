# Hardcore FX Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptation of the MetaTrader "HardcoreFX" expert. The strategy tracks ZigZag pivot highs and lows and opens positions when price breaks beyond them. It applies fixed stop loss and take profit levels and also trails the position to protect accrued gains.

## Details
- **Entry Criteria**: Close above last ZigZag high to go long; close below last ZigZag low to go short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit or trailing stop hit.
- **Stops**: Fixed stop loss, take profit and trailing stop.
- **Default Values**:
  - `ZigzagLength` = 17
  - `StopLoss` = 1400
  - `TakeProfit` = 5400
  - `TrailingStop` = 500
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: Stop loss, Take profit, Trailing stop
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
