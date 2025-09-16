# Center of Gravity Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Center of Gravity indicator that multiplies SMA and WMA and smooths the result. A long position opens when the center line crosses above its smoothed average and a short position opens on the opposite crossover. Positions are closed when the signal flips against the current direction.

## Details

- **Entry Criteria**: Center line crosses its smoothed average
- **Long/Short**: Both
- **Exit Criteria**: Signal changes side
- **Stops**: No
- **Default Values**:
  - `CandleType` = H4
  - `Period` = 10
  - `SmoothPeriod` = 3
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: SMA, WMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
