# Hurst Exponent
[Русский](README_ru.md) | [中文](README_cn.md)

Simple strategy that trades based on a smoothed Hurst exponent.  
The Hurst value is smoothed with an EMA and compared to a threshold to determine market regime.

## Details
- **Entry Criteria**:
  - **Long**: Smoothed Hurst > Threshold
  - **Short**: Smoothed Hurst < Threshold
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Smoothed Hurst < Threshold
  - **Short**: Smoothed Hurst > Threshold
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `HurstPeriod = 100`
  - `SmoothLength = 10`
  - `Threshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5)`
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Hurst Exponent, EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
