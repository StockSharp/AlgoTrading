# Statistical Arbitrage Spread Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the spread between two correlated instruments. A long position in the first security is opened when the spread falls below its mean by a multiple of the spread's standard deviation. The position is closed once the spread returns to the mean.

## Details
- **Entry Criteria**:
  - Long: Spread < Mean - Multiplier * StdDev
- **Long/Short**: Long only
- **Exit Criteria**: Close when spread > Mean
- **Stops**: No
- **Default Values**:
  - `LookbackPeriod` = 20
  - `StdMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Arbitrage
  - Direction: Long
  - Indicators: Spread statistics
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
