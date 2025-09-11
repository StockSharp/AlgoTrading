[Русский](README_ru.md) | [中文](README_cn.md)

Trend Vanguard uses a simple ZigZag on highs and lows to follow trend reversals.
It flips direction when the ZigZag changes orientation.

## Details

- **Entry Criteria**: ZigZag reversal
- **Long/Short**: Both
- **Exit Criteria**: Opposite ZigZag signal
- **Stops**: No
- **Default Values**:
  - `Depth` = 21
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
