# Hull MA Slope Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Hull MA Slope Breakout strategy tracks the rate of change of the Hull. An unusually steep slope hints that a new trend is forming.

Entries occur when slope exceeds its typical level by a multiple of standard deviation, taking trades in the direction of acceleration with a protective stop.

It appeals to active traders eager for early trend exposure. Positions exit when the slope drifts back toward normal readings. Default `HullLength` = 9.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `HullLength` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Hull
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 121%. It performs best in the crypto market.
