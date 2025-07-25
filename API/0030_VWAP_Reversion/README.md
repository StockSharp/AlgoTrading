# VWAP Reversion
[Русский](README_ru.md) | [中文](README_cn.md)
 
VWAP Reversion strategy that trades on deviations from Volume Weighted Average Price

VWAP Reversion trades deviations from the volume-weighted average price. If price strays too far above or below VWAP, the strategy fades the move and exits on a snap back.

Because VWAP reflects typical transaction levels, extreme deviations often lure price back toward it. Some traders combine this signal with intraday trend filters for higher probability.


## Details

- **Entry Criteria**: Signals based on RSI, VWAP.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `DeviationPercent` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI, VWAP
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 127%. It performs best in the stocks market.
