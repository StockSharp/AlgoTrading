# First Day of Month Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Many markets exhibit a bullish bias on the first trading day of the month as new capital flows into funds.
Traders attempt to front-run this effect by buying at the prior month's close or early in the session.

The strategy enters long at the start of the month and exits before the second day begins, capturing the typical influx of buying.

A small stop protects against downside surprises if the expected strength fails to appear.

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
