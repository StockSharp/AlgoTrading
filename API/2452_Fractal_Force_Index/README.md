# Fractal Force Index Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on a smoothed Force Index crossing user-defined levels. When the indicator rises above the high level or falls below the low level, the strategy opens or closes positions depending on the selected trading mode. The Force Index is calculated from the price change and volume and smoothed with an EMA.

## Details

- **Entry Criteria**
  - *Direct mode*:
    - **Long**: indicator crosses above `HighLevel`.
    - **Short**: indicator crosses below `LowLevel`.
  - *Against mode*:
    - **Long**: indicator crosses below `LowLevel`.
    - **Short**: indicator crosses above `HighLevel`.
- **Exit Criteria**
  - *Direct mode*:
    - **Long**: cross below `LowLevel`.
    - **Short**: cross above `HighLevel`.
  - *Against mode*:
    - **Long**: cross above `HighLevel`.
    - **Short**: cross below `LowLevel`.
- **Stops**: No.
- **Default Values**:
  - `Period` = 30
  - `HighLevel` = 0
  - `LowLevel` = 0
  - `Candle Type` = 4-hour
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Force Index
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
