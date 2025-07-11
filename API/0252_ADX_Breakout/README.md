# ADX Breakout

The ADX Breakout strategy monitors the ADX for strong expansions. When readings jump beyond their typical range, price often starts a new move.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the ADX falls back toward the mean. Defaults start with `ADXPeriod` = 14.

## Rules

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `ADXPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
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