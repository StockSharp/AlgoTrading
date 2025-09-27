# OBV ATR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks On-Balance Volume (OBV) and enters trades when OBV breaks its recent high or low. It maintains a dynamic channel similar to an ATR breakout, switching between bull and bear modes.

## Details

- **Entry Criteria**: OBV crosses above previous high for long; crosses below previous low for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or protective orders.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: OBV, Highest, Lowest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
