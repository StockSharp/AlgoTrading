# Internal Bar Strength IBS Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Short-only mean reversion strategy using Internal Bar Strength (IBS). Shorts when IBS is high and price breaks above the previous high, exits when IBS falls below a lower threshold.

## Details

- **Entry Criteria**: IBS >= upper threshold and close > previous high
- **Long/Short**: Short
- **Exit Criteria**: IBS <= lower threshold
- **Stops**: No
- **Default Values**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **Filters**:
  - Category: Mean Reversion
  - Direction: Short
  - Indicators: IBS
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
