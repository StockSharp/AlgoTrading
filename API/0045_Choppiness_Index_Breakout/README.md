# Choppiness Index Breakout

The Choppiness Index gauges whether the market is trending or ranging. When the indicator drops below a threshold, it signals the start of a trend from a choppy environment.

This strategy enters in the direction of price relative to its moving average when choppiness falls. It exits if choppiness rises back above a high threshold or a stop-loss hits.

The goal is to catch new trends emerging from consolidation periods.

## Details

- **Entry Criteria**: Choppiness below `ChoppinessThreshold` with price above/below MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Choppiness above `HighChoppinessThreshold` or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Choppiness, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
