# Elder Impulse
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Elder's Impulse System

Testing indicates an average annual return of about 106%. It performs best in the stocks market.

Elder Impulse combines EMA direction with MACD histogram color. Green bars above the EMA prompt longs, red bars below prompt shorts, and neutral bars signal exits.

By blending trend direction and momentum, this approach keeps traders on the right side of strong moves. Exits are straightforward, relying on the histogram color change or the EMA slope reversing.


## Details

- **Entry Criteria**: Signals based on MACD.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

