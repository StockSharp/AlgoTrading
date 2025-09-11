# Moving Average Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Buys when the short SMA crosses above the long SMA and sells when it crosses below. Positions reverse on opposite signals.

## Details

- **Entry Criteria**:
  - Long when short SMA crosses above long SMA.
  - Short when short SMA crosses below long SMA.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse on opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Crossover
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

