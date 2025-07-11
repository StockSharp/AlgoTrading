# CCI Slope Mean Reversion

The CCI Slope Mean Reversion strategy focuses on extreme readings of the CCI to exploit reversion. Wide departures from the typical level rarely last.

Trades trigger when the indicator swings far from its mean and then begins to reverse. Both long and short setups include a protective stop.

Suited for swing traders expecting oscillations, the strategy closes out once the CCI returns toward balance. Starting parameter `CciPeriod` = 20.

## Rules

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `CciPeriod` = 20
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium