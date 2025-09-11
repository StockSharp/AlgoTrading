# Uptrick Intensity Index Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes the Trend Intensity Index from three moving averages and trades on crossovers of TII and its own moving average.

## Details

- **Entry Criteria**: TII crosses above its SMA (buy) or below (sell)
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `Ma3Length` = 50
  - `TiiMaLength` = 50
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, TII
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
