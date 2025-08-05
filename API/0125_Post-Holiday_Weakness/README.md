# Post-Holiday Weakness Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Post-Holiday Weakness is the tendency for prices to drift lower immediately after a major holiday when volume remains thin.
With many participants still away, counter-trend moves can gain traction.

Testing indicates an average annual return of about 112%. It performs best in the forex market.

The strategy sells short the day after a holiday and covers quickly once normal participation returns.

A small stop is used to avoid excessive losses during low-liquidity trading.

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

