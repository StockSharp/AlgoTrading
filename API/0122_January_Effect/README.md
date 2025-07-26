# January Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The January Effect observes that small-cap stocks often outperform early in the year, possibly due to tax-loss selling in December.
Traders attempt to capture this tendency by buying in late December and selling after the first few weeks of January.

The strategy follows that schedule, entering near year-end and exiting mid-January.

A stop-loss ensures losses stay manageable if the effect fails to appear.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 103%. It performs best in the stocks market.
