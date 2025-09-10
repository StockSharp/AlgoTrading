# Three Down Three Up Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys after a specified number of consecutive down closes and closes the position after a run of up closes. An optional EMA filter allows entries only when price is above the moving average.

## Details

- **Entry Criteria**: Price closes lower than previous bar for N bars. Optional condition price above EMA.
- **Exit Criteria**: Price closes higher than previous bar for M bars.
- **Long/Short**: Long only.
- **Stops**: None.
- **Default Values**: Buy trigger = 3, sell trigger = 3, EMA period = 200.
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: EMA (optional)
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
