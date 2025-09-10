# AI Supertrend Pivot Percentile Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines two Supertrend indicators with an ADX filter and a Williams %R pivot percentile filter. A long position is opened when price is above both Supertrends, ADX confirms a strong trend and Williams %R is above -50. Short positions use the opposite conditions.

## Details

- **Entry Criteria**:
  - **Long**: Price above both Supertrends, ADX > threshold, Williams %R > -50.
  - **Short**: Price below both Supertrends, ADX > threshold, Williams %R < -50.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: Percent-based take-profit and stop-loss.
- **Default Values**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend, ADX, Williams %R
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
