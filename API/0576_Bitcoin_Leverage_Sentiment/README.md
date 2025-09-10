# Bitcoin Leverage Sentiment Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy analyzes the Z-Score of the ratio between Bitcoin long and short positions. A long trade is opened when the Z-Score crosses above a configurable threshold and closed when it crosses below the long exit level. Short trades use mirrored thresholds. Trading direction can be limited to long, short, or both sides.

## Details

- **Entry Criteria**:
  - Z-Score crosses above long entry threshold → long.
  - Z-Score crosses below short entry threshold → short.
- **Long/Short**: Configurable
- **Exit Criteria**:
  - Z-Score crosses below long exit threshold.
  - Z-Score crosses above short exit threshold.
- **Stops**: None
- **Default Values**:
  - Z-Score length = 252
  - Long entry = 1.0
  - Long exit = -1.618
  - Short entry = -1.618
  - Short exit = 1.0
  - Candle type = 1 day
- **Filters**:
  - Category: Sentiment
  - Direction: Both
  - Indicators: SMA, StdDev
  - Stops: None
  - Complexity: Low
  - Timeframe: Long
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
