# Exchange Price Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy compares the current closing price with prices several bars ago over two lookback periods. A long position is opened when the short-term change rises above the long-term change; a short position is opened when the opposite crossover occurs.

## Details

- **Entry Criteria**: short-term price difference crossing above/below long-term difference
- **Long/Short**: Both
- **Exit Criteria**: opposite crossover
- **Stops**: No
- **Default Values**:
  - `ShortPeriod` = 96
  - `LongPeriod` = 288
  - `CandleType` = 8-hour candles
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Price difference
  - Stops: No
  - Complexity: Basic
  - Timeframe: 8-hour
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
