# Dual SuperTrend w VIX Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines two SuperTrend indicators with a VIX-based volatility filter. A long position opens when both SuperTrends are bullish and the VIX index is above its mean. A short position opens when both SuperTrends are bearish and the VIX is rising above its average plus a standard-deviation buffer. Positions are closed when either SuperTrend flips direction.

## Details

- **Entry Criteria**:
  - **Long**: Both SuperTrends indicate an uptrend and VIX is above its mean.
  - **Short**: Both SuperTrends indicate a downtrend and VIX is above its mean and rising.
- **Exit Criteria**:
  - Opposite SuperTrend signal.
- **Stops**: None.
- **Default Values**:
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend, SMA, StandardDeviation, EMA
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
