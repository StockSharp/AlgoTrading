# Hull MA Slope Mean Reversion

The Hull MA Slope Mean Reversion strategy focuses on extreme readings of the Hull to exploit reversion. Wide departures from the normal level rarely last.

Trades trigger when the indicator swings far from its mean and then begins to reverse. Both long and short setups include a protective stop.

Suited for swing traders expecting oscillations, the strategy closes out once the Hull returns toward balance. Starting parameter `HullPeriod` = 9.

## Rules

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `HullPeriod` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Hull
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium