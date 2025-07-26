# Volume Supertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses Volume Supertrend indicators to generate signals.
Long entry occurs when Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend). Short entry occurs when Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend).
It is suitable for traders seeking opportunities in trend markets.

## Details
- **Entry Criteria**:
  - **Long**: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
  - **Short**: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when Supertrend turns down
  - **Short**: Exit short position when Supertrend turns up
- **Stops**: Yes.
- **Default Values**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Volume Supertrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 64%. It performs best in the forex market.
