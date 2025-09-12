# Triangular Hull Moving Average
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Hull Moving Average cross with a two-bar lag.

The strategy compares the Hull Moving Average with its value two bars ago. A crossover upwards opens a long position, while a downward cross opens a short one. Direction can be limited to long-only or short-only modes.

## Details
- **Entry Criteria**: HMA cross with 2-bar lag.
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite signal or direction filter.
- **Stops**: No.
- **Default Values**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: MA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
