# Droneox Equity Guardian
[Русский](README_ru.md) | [中文](README_cn.md)

Equity protection strategy that monitors account equity and closes all positions once the equity reaches a defined target or falls below a stop level. It can optionally stop further trading after closing positions.

## Details

- **Entry Criteria**: None; works as a protective overlay.
- **Long/Short**: Closes positions in both directions.
- **Exit Criteria**: Equity hitting the target or stop level.
- **Stops**: Equity-based stop and target.
- **Default Values**:
  - `EquityTarget` = 999999m
  - `EquityStop` = 0m
  - `ClosePositions` = true
  - `DisableTrading` = true
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: None
  - Stops: Equity Stop/Target
  - Complexity: Beginner
  - Timeframe: None
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
