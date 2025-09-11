# Indices Sector Sigma Spikes Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy scans sector indices and logs sigma spikes based on return volatility.

## Details

- **Entry Criteria**: None, screening only.
- **Long/Short**: None.
- **Exit Criteria**: None.
- **Stops**: No.
- **Default Values**:
  - `LookbackPeriod` = 20.
  - `ReturnPeriod` = 20.
  - `SigmaThreshold` = 2.
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Indicator
  - Direction: None
  - Indicators: StdDev
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
