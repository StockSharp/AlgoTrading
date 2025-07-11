# Parabolic SAR Distance Mean Reversion

The Parabolic SAR Distance Mean Reversion strategy focuses on extreme readings of the Parabolic to exploit reversion. Wide departures from the normal level rarely last.

Trades trigger when the indicator swings far from its mean and then begins to reverse. Both long and short setups include a protective stop.

Suited for swing traders expecting oscillations, the strategy closes out once the Parabolic returns toward balance. Starting parameter `AccelerationFactor` = 0.02m.

## Rules

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `AccelerationFactor` = 0.02m
  - `AccelerationLimit` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Parabolic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium