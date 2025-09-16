# Fisher Org Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Fisher Transform indicator with predefined upper and lower levels. A long position is opened when the Fisher value crosses above the lower level. A short position is opened when the Fisher value crosses below the upper level.

## Details

- **Entry Criteria**:
  - **Long**: `Fisher crosses above DownLevel`
  - **Short**: `Fisher crosses below UpLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite signal triggers position reversal
- **Stops**: No
- **Default Values**:
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Fisher Transform
  - Stops: No
  - Complexity: Low
  - Timeframe: Medium-term (H4)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
