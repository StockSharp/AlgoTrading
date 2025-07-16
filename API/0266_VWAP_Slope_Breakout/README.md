# VWAP Slope Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The VWAP Slope Breakout strategy observes the rate of change of the VWAP. An unusually steep slope hints that a new trend is forming.

Entries occur when slope exceeds its typical level by a multiple of standard deviation, taking trades in the direction of acceleration with a protective stop.

It appeals to active traders eager for early trend exposure. Positions exit when the slope drifts back toward normal readings. Default `LookbackPeriod` = 20.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: VWAP
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
