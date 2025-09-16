# MAVA Xonax Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses exponential moving averages of open and close prices to detect direction changes. Stop loss and take profit distances are derived from high and low EMAs, ensuring trades have predefined risk and reward levels.

## Details

- **Entry Criteria**:
  - **Long**: EMA of open crosses above EMA of close using the last two completed bars.
  - **Short**: EMA of open crosses below EMA of close using the last two completed bars.
- **Long/Short**: Both
- **Stops**: Fixed stop loss and take profit based on EMA ranges.
- **Default Values**:
  - `EmaPeriod` = 6
  - `CandleType` = TimeSpan.FromMinutes(240).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Long-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
