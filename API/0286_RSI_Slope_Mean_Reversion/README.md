# RSI Slope Mean Reversion

The RSI Slope Mean Reversion strategy focuses on extreme readings of the RSI to exploit reversion. Wide departures from the average level rarely last.

Trades trigger when the indicator swings far from its mean and then begins to reverse. Both long and short setups include a protective stop.

Suited for swing traders expecting oscillations, the strategy closes out once the RSI returns toward balance. Starting parameter `RsiPeriod` = 14.

## Rules

- **Entry Criteria**: Indicator crosses back toward mean.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium