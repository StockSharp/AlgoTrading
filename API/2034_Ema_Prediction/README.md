# EMA Prediction Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the EMA Prediction indicator which generates signals when fast and slow exponential moving averages cross on a candle confirming the direction.

The strategy opens long positions when the fast EMA crosses above the slow EMA during a bullish candle and closes any short position. It opens short positions when the fast EMA crosses below the slow EMA during a bearish candle and closes any long position.

## Details

- **Entry Criteria**:
  - Long: Fast EMA crosses above Slow EMA and the candle is bullish.
  - Short: Fast EMA crosses below Slow EMA and the candle is bearish.
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: Fixed take profit and stop loss
- **Default Values**:
  - `CandleType` = 6-hour candles
  - `FastPeriod` = 1
  - `SlowPeriod` = 2
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
- **Filters**:
  - Category: Moving average crossover
  - Direction: Both
  - Indicators: EMA
  - Stops: Take profit & stop loss
  - Complexity: Basic
  - Timeframe: 6-hour
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
