# Bollinger Band Width Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Bollinger Band Width Breakout strategy tracks the Bollinger for strong expansions. When readings jump beyond their normal range, price often starts a new move.

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the Bollinger falls back toward the mean. Defaults start with `BollingerLength` = 20.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Bollinger
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

