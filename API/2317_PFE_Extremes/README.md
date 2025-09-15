# PFE Extremes
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of the Polarized Fractal Efficiency (PFE) indicator. When PFE crosses above the upper level, the strategy closes any short position and opens a long. When PFE crosses below the lower level, it closes long positions and opens a short.

The PFE indicator evaluates how efficiently price is moving relative to its path. Values near +1 suggest strong upward movement, while values near -1 show strong downward movement. Threshold crossings may highlight the start of a new trend.

## Details

- **Entry Criteria**: PFE crosses above `UpLevel` for longs or below `DownLevel` for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite level break or reversal signal.
- **Stops**: Not used by default; can be added via position protection.
- **Default Values**:
  - `PfePeriod` = 5
  - `UpLevel` = 0.5
  - `DownLevel` = -0.5
  - `CandleType` = 4-hour timeframe
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: PFE
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
