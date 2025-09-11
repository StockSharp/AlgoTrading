# Intraday Volume Swings Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades when price enters volume-defined swing regions from the current or previous day.

## Details

- **Entry Criteria**:
  - **Long**: Price pushes into the high swing region.
  - **Short**: Price pushes into the low swing region.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `RegionMustClose` = true
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume
  - Stops: No
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
