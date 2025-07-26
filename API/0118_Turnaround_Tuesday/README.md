# Turnaround Tuesday Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Turnaround Tuesday refers to the tendency for markets that sold off on Monday to rebound the next day.
The effect is often attributed to traders overreacting after the weekend and then reversing course.

Testing indicates an average annual return of about 91%. It performs best in the stocks market.

This strategy buys at Tuesday's open when Monday was down, holding only for the session or until a modest profit target is reached.

Stops are tight to protect against continued weakness if the bounce fails to develop.

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

