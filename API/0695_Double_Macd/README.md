# Double MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Double MACD uses two MACD indicators with different speeds. A position is opened only when both MACDs agree on direction.

The first MACD is fast and reacts quickly. The second is slower and confirms the trend before trades are taken.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Both MACD lines above their signal lines.
  - **Short**: Both MACD lines below their signal lines.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Optional stop loss.
- **Default Values**:
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
