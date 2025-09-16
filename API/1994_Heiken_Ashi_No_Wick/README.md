# Heiken Ashi No Wick Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades against Heiken Ashi candles that appear without wicks. A bullish Heiken Ashi candle whose body is larger than the previous one and lacks a lower shadow triggers a short entry. A bearish candle with a longer body and no upper shadow opens a long. Positions close when an opposite candle without the respective wick forms.

## Details

- **Entry Criteria**: bullish HA without lower wick and body larger than previous for shorts; bearish HA without upper wick and body larger than previous for longs
- **Long/Short**: Long & Short
- **Exit Criteria**: opposite colored HA candle without wick
- **Stops**: No
- **Default Values**:
  - `CandleType` = 15-minute candles
- **Filters**:
  - Category: Pattern
  - Direction: Reversal
  - Indicators: Heikin-Ashi
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
