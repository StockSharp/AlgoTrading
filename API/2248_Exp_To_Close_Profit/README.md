# Exp To Close Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that monitors open profit and closes all positions once the specified profit target is achieved. Designed to be attached alongside other strategies to lock in gains.

## Details

- **Entry Criteria**: None, the strategy never opens positions.
- **Long/Short**: Both directions are supported for closing.
- **Exit Criteria**: When `MaxProfit` is reached, existing position is closed.
- **Stops**: Profit target only.
- **Default Values**:
  - `MaxProfit` = 1000
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
