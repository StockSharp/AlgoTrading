# Color Bears Gap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements a strategy based on the Color Bears Gap indicator. The indicator compares two smoothed gaps between the high price and smoothed open/close values. When the difference crosses zero, positions are opened in the new direction and opposite positions are closed.

## Details
- **Entry Criteria**: Indicator crosses below zero -> buy, crosses above zero -> sell.
- **Long/Short**: Configurable via parameters.
- **Exit Criteria**: Opposite zero crossing.
- **Stops**: None.
- **Default Values**:
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 8-hour timeframe
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Color Bears Gap
  - Stops: No
  - Complexity: Medium
  - Timeframe: 8-hour
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
