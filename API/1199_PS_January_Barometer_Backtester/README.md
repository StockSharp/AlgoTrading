# PS January Barometer Backtester Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the January Barometer where a long position is taken when the close in February–June exceeds the January high. Optional filters require a positive Santa Claus Rally and/or First Five Days return.

## Details

- **Entry Criteria**: February–June close above January high with optional seasonal filters
- **Long/Short**: Long
- **Exit Criteria**: close position in December
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1 month
  - `UseSantaClausRally` = false
  - `UseFirstFiveDays` = false
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: Seasonality
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Monthly
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
