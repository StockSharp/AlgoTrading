# Ultimate Strategy Template
[Русский](README_ru.md) | [中文](README_cn.md)

Basic moving average crossover template that opens long or short positions when fast and slow averages cross. Includes optional percent stop loss and take profit protections.

## Details

- **Entry Criteria**: Fast SMA crossing the slow SMA.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossover or risk protections.
- **Stops**: Percent stop loss and take profit.
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Medium
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
