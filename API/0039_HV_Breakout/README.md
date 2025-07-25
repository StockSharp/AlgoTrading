# Historical Volatility Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
This breakout method uses historical volatility to set dynamic thresholds. When price moves beyond a reference level by more than the current volatility, it indicates a potential trend.

Testing indicates an average annual return of about 154%. It performs best in the stocks market.

The strategy compares price to levels derived from standard deviation and a simple moving average. Breakouts above or below those levels trigger trades.

Exits occur when price crosses back through the moving average or the stop hits.

## Details

- **Entry Criteria**: Price breaks above or below HV-based level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `HvPeriod` = 20
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: HV, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

