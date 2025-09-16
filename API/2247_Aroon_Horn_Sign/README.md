# Aroon Horn Sign
[Русский](README_ru.md) | [中文](README_cn.md)

The **Aroon Horn Sign** strategy looks for trend reversals using the Aroon indicator.
It monitors the Aroon Up and Down lines on higher timeframe candles. When the
Aroon Up line crosses above the Aroon Down line and stays above the 50 level,
this signals a potential bullish reversal. The strategy closes any short
position and opens a new long. Conversely, when Aroon Down dominates above 50,
any existing long is closed and a short position is initiated.

The approach uses fixed take-profit and stop-loss levels expressed in price
units. These levels are activated through the built-in risk protection module.
Because the logic relies only on the Aroon values, it works across different
markets and timeframes without additional filters.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: `Aroon Up` > `Aroon Down` and `Aroon Up` >= 50.
  - **Short**: `Aroon Down` > `Aroon Up` and `Aroon Down` >= 50.
- **Exit Criteria**:
  - Long positions close when a short entry condition appears.
  - Short positions close when a long entry condition appears.
- **Stops**: Fixed stop-loss and take-profit using `StartProtection`.
- **Default Values**:
  - `AroonPeriod` = 9
  - `CandleType` = 4‑hour candles
  - `TakeProfit` = 2000 (price units)
  - `StopLoss` = 1000 (price units)
- **Filters**:
  - Category: Trend reversal
  - Direction: Long and Short
  - Indicators: Aroon
  - Complexity: Simple
  - Risk level: Medium
