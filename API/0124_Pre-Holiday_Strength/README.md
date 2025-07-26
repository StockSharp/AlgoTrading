# Pre-Holiday Strength Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Pre-Holiday Strength refers to the bullish tendency just before major market holidays when volume is lighter and sentiment optimistic.
Traders often position ahead of the break, pushing prices higher in the final session or two.

The strategy goes long on the day before a holiday and exits the following session or at the close, capturing that short-term bias.

A tight stop is used in case the expected lift doesn't occur.

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

Testing indicates an average annual return of about 109%. It performs best in the crypto market.
