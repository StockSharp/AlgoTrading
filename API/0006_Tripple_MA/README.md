# Tripple MA
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Triple Moving Average crossover

Testing indicates an average annual return of about 55%. It performs best in the stocks market.

Triple MA aligns three moving averages to define direction. When the shortest average is above the middle and long averages a long entry occurs. The reverse alignment opens shorts, and a cross of the short and middle lines closes the trade.

Using three averages helps filter out noise present in single-MA systems. This layered approach seeks to confirm momentum before committing to a trade.


## Details

- **Entry Criteria**: Signals based on MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `ShortMaPeriod` = 5
  - `MiddleMaPeriod` = 20
  - `LongMaPeriod` = 50
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

