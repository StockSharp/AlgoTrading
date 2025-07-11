# Parabolic SAR Distance Breakout

The Parabolic SAR Distance Breakout strategy watches the Parabolic for rapid expansions. When readings jump beyond their recent range, price often starts a new move.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the Parabolic falls back toward the mean. Defaults start with `Acceleration` = 0.02m.

## Rules

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Parabolic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium