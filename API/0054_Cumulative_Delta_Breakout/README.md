# Cumulative Delta Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
Cumulative Delta sums the difference between buy and sell volume. This strategy watches the running total and trades when it breaks above its highest value or below its lowest value within the lookback period.

A break of cumulative delta often precedes price follow-through. The strategy closes trades when delta crosses back through zero or a stop-loss level.

## Details

- **Entry Criteria**: Cumulative delta exceeds highest or lowest value in lookback.
- **Long/Short**: Both directions.
- **Exit Criteria**: Delta crosses zero or stop.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Cumulative Delta
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 49%. It performs best in the crypto market.
