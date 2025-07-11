# ADX Slope Breakout

The ADX Slope Breakout strategy tracks the rate of change of the ADX. An unusually steep slope hints that a new trend is forming.

Entries occur when slope exceeds its typical level by a multiple of standard deviation, taking trades in the direction of acceleration with a protective stop.

It appeals to active traders eager for early trend exposure. Positions exit when the slope drifts back toward normal readings. Default `AdxPeriod` = 14.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `AdxPeriod` = 14
  - `SlopePeriod` = 20
  - `BreakoutMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
