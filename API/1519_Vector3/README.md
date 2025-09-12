# Vector3 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades based on alignment of three moving averages.
Goes long when fast > middle > slow and short when fast < middle < slow.

## Details

- **Entry Criteria**: fast MA above middle and middle above slow (long); inverse for short
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
