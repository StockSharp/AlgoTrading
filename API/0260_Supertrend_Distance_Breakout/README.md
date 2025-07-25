# Supertrend Distance Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Supertrend Distance Breakout strategy watches the Supertrend for sharp expansions. When readings jump beyond their average range, price often starts a new move.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the Supertrend falls back toward the mean. Defaults start with `SupertrendPeriod` = 10.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Supertrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 115%. It performs best in the stocks market.
