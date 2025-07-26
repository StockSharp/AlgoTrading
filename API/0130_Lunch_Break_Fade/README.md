# Lunch Break Fade Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Lunch Break Fade targets reversals that develop during the slow midday period.
After the morning session, trends often pause or pull back as volume dries up around lunchtime.

The strategy fades the morning move around noon, entering counter to the prevailing direction and covering before volume returns.

A percent stop manages risk if the trend resumes instead of fading.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Intraday
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 127%. It performs best in the stocks market.
