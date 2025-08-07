# Heikin Ashi Universal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This universal template converts standard candles into Heikin Ashi ones and trades in the direction of their body. The method smooths price noise, allowing trends to appear more clearly. It is lightweight and can serve as a base for custom filters or exits.

The system enters long when the Heikin Ashi close is above its open and flips short when the close falls below the open.

## Details

- **Entry Criteria**:
  - **Long**: `HA_Close > HA_Open`
  - **Short**: `HA_Close < HA_Open`
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Heikin Ashi
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
