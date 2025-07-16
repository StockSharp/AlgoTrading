# MA Slope Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The MA Slope Breakout strategy watches the rate of change of the MA. An unusually steep slope hints that a new trend is forming.

Entries occur when slope exceeds its typical level by a multiple of standard deviation, taking trades in the direction of acceleration with a protective stop.

It appeals to active traders eager for early trend exposure. Positions exit when the slope drifts back toward normal readings. Default `MaLength` = 20.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `MaLength` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
