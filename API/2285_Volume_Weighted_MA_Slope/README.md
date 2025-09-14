# Volume Weighted MA Slope
[Русский](README_ru.md) | [中文](README_cn.md)

The **Volume Weighted MA Slope** strategy analyzes the direction of the Volume Weighted Moving Average (VWMA). The system enters a long position when the VWMA rises for two consecutive bars and opens a short position when the VWMA declines for two bars. Existing positions are closed once the indicator slope reverses.

This approach attempts to follow emerging trends by using volume-adjusted price averages, filtering out moves that occur on low volume.

## Details

- **Entry Criteria**: VWMA rising for two bars (long) or falling for two bars (short).
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite VWMA slope.
- **Stops**: Yes (configurable, default 1% stop loss / 2% take profit).
- **Default Values**:
  - `VwmaPeriod` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: VWMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
