# Bj Candle Patterns Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy looks for Dragonfly Doji and Gravestone Doji candlestick patterns. A Dragonfly Doji with a long lower wick can signal bullish reversal, while a Gravestone Doji with a long upper wick may indicate bearish reversal. The strategy buys after a Dragonfly Doji and sells after a Gravestone Doji.

## Details

- **Entry Criteria**: Dragonfly Doji → long; Gravestone Doji → short.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or discretionary.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 15 minute
  - `DojiThreshold` = 0.1
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
