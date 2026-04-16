# Ratio Pairs Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This pairs trading strategy tracks the price ratio (Asset1 / Asset2) between two
correlated instruments. A Bollinger Bands indicator is applied to the ratio to
obtain its rolling mean and the standard-deviation channel. The distance between
the current ratio and its mean is converted into a z-score that drives entries
and exits.

When the z-score exceeds the entry threshold the ratio is considered too high
relative to the pair's equilibrium, so the strategy sells Asset1 and buys
Asset2. When the z-score falls below the negative threshold the opposite pair
is opened. Positions are closed as soon as the z-score returns inside the exit
band, meaning the ratio has reverted toward its mean.

The `HedgeRatio` parameter controls the size of the second leg so the pair can
be kept approximately dollar- or beta-neutral. A percentage stop-loss is applied
to each leg in case the relationship breaks down temporarily.

## Details
- **Entry Criteria**:
  - **Long pair (Long Asset1 / Short Asset2)**: Ratio Z-Score <= -`EntryZScore`
  - **Short pair (Short Asset1 / Long Asset2)**: Ratio Z-Score >= `EntryZScore`
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Absolute Z-Score of the ratio drops to `ExitZScore` or below.
- **Stops**: Yes, percentage stop-loss on each leg.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `EntryZScore` = 2.0m
  - `ExitZScore` = 0.5m
  - `HedgeRatio` = 1.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Bollinger Bands on ratio, Z-Score
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium
