# VIX Spike Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Buys when the VIX index spikes above its moving average by a multiple of standard deviation and closes after a fixed number of bars.

## Details

- **Entry Criteria**: VIX > mean + StdDevMultiplier * standard deviation.
- **Long/Short**: Long only.
- **Exit Criteria**: Exit after `ExitPeriods` bars.
- **Stops**: Yes.
- **Default Values**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: SMA, StdDev
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
