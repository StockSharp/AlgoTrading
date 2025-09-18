# Support Resist Trade Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy ported from MetaTrader that combines a long-term EMA trend filter with dynamic support and resistance levels. It looks back over the recent swing range, waits for price to break the prior ceiling or floor in the direction of the trend, and manages positions with staged pip-based trailing stops.

## Details

- **Entry Criteria**: price closes beyond the previous `Lookback`-period high (long) or low (short) and the bar opens above/below the EMA `MaPeriod`
- **Long/Short**: Both
- **Exit Criteria**: trailing stop hits or a profitable position crosses back through the refreshed support/resistance band
- **Stops**: initial stop at the opposite band, trail after +20/+40/+60 pip moves (locking 10/20/30 pips respectively)
- **Default Values**:
  - `Lookback` = 55
  - `MaPeriod` = 500
  - `CandleType` = 1 minute
  - `OrderVolume` = 0.1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, Highest, Lowest
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
