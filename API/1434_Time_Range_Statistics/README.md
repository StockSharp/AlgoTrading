# Time Range Statistics Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Collects simple statistics between selected bar indices.
Logs mean price, normalized range, percent change, average volume and gap count.
Trades long if the period ends positive and short if negative.

## Details

- **Entry Criteria**: percent change at `EndIndex` determines direction
- **Long/Short**: Both
- **Exit Criteria**: none
- **Stops**: No
- **Default Values**:
  - `StartIndex` = 9000
  - `EndIndex` = 10000
- **Filters**:
  - Category: Statistics
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
