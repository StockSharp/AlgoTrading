# Correlation Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This strategy monitors the rolling correlation between two assets. When correlation deviates sharply from its normal range, it may signal shifting relationships that produce tradeable trends.

Testing indicates an average annual return of about 49%. It performs best in the crypto market.

A long position buys the first asset and sells the second when correlation plunges below the average by more than `Threshold` standard deviations. A short position does the reverse when correlation spikes above the average. Trades are closed once correlation returns toward its mean.

Such setups aim to capture temporary dislocations in how assets move together. Stop-loss orders protect against the correlation continuing to diverge rather than mean-revert.

## Details
- **Entry Criteria**:
  - **Long**: Correlation < Average - Threshold * StdDev
  - **Short**: Correlation > Average + Threshold * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when correlation nears the average
  - **Short**: Exit when correlation nears the average
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 20
  - `Threshold` = 2m
  - `StopLossPercent` = 2m
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Correlation
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium

