# IMACD Sniper
[Русский](README_ru.md) | [中文](README_cn.md)

IMACD Sniper combines MACD crossovers with an EMA trend filter, volume confirmation, and strong candle patterns. Dynamic take profit and stop loss are based on the recent average range.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: MACD line crosses above signal line, price above EMA, MACD delta > min delta, both lines far from zero, volume above average, strong bullish candle.
  - **Short**: MACD line crosses below signal line, price below EMA, MACD delta > min delta, both lines far from zero, volume above average, strong bearish candle.
- **Exit Criteria**: Opposite MACD cross or reaching take profit / stop loss.
- **Stops**: Dynamic take profit and stop loss based on average range.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: MACD, EMA, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
