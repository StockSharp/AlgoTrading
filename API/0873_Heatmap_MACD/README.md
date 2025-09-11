# Heatmap MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Heatmap MACD trades when MACD histograms from five timeframes align. A long position is opened when all histograms turn positive, and a short position when all turn negative. Optionally, the position can be closed when any histogram flips against the trade.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: MACD histogram > 0 on all five timeframes and previously not all positive.
  - **Short**: MACD histogram < 0 on all five timeframes and previously not all negative.
- **Exit Criteria**: Opposite signal or optional close on opposite.
- **Stops**: None by default.
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TimeFrame1` = tf(60)
  - `TimeFrame2` = tf(120)
  - `TimeFrame3` = tf(240)
  - `TimeFrame4` = tf(240)
  - `TimeFrame5` = tf(480)
  - `CloseOnOpposite` = false
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: MACD
  - Stops: No
  - Complexity: Basic
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
