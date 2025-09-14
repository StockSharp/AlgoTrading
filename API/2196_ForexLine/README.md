# Forex Line Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Forex Line strategy is a trend-following system derived from the MetaTrader indicator "ForexLine". It applies two stages of weighted moving averages to the price to build fast and slow lines. Crossovers between these double-smoothed lines are used to determine entry signals.

The strategy buys when the fast line crosses above the slow line and sells when the fast line crosses below the slow line. Each moving average uses a two-step smoothing process that helps filter market noise.

## Details

- **Entry Criteria**:
  - **Long**: Fast double-smoothed WMA crosses above the slow double-smoothed WMA.
  - **Short**: Fast double-smoothed WMA crosses below the slow double-smoothed WMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite crossover closes existing position.
- **Stops**: Not included; can be added externally.
- **Default Values**:
  - `FastLength1` = 5
  - `FastLength2` = 10
  - `SlowLength1` = 20
  - `SlowLength2` = 20
  - `CandleType` = 8 hour timeframe
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Weighted moving averages
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
