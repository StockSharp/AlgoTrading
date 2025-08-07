# Double RSI
[Русский](README_ru.md) | [中文](README_cn.md)

Double RSI uses two Relative Strength Index calculations: one on the trading
chart and another on a higher timeframe. Trades are taken only when both RSI
readings support the same direction, aligning short‑term entries with longer‑term
momentum.

The main timeframe looks for RSI crossing out of overbought or oversold zones.
If the higher‑timeframe RSI confirms the move, the strategy opens a position.
An optional take‑profit can lock in gains after a predefined move.

## Details
- **Data**: Price candles on two timeframes.
- **Entry Criteria**:
  - **Long**: Lower‑timeframe RSI exits oversold AND higher‑timeframe RSI is bullish.
  - **Short**: Lower‑timeframe RSI exits overbought AND higher‑timeframe RSI is bearish.
- **Exit Criteria**: Opposite RSI signal or take‑profit if `UseTP` is true.
- **Stops**: None by default.
- **Default Values**:
  - `CandleType` = tf(5)
  - `RSILength` = 14
  - `MTFTimeframe` = tf(15)
  - `UseTP` = False
- **Filters**:
  - Category: Momentum
  - Direction: Long & Short
  - Indicators: RSI (multi‑timeframe)
  - Complexity: Moderate
  - Risk level: Medium
