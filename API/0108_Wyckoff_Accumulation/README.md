# Wyckoff Accumulation Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Wyckoff Accumulation describes a basing phase where large interests quietly build positions after a decline.
Volume and price action form a series of tests of support followed by higher lows, hinting at growing demand.

Testing indicates an average annual return of about 61%. It performs best in the crypto market.

This strategy enters long when price breaks out of the accumulation range, expecting a new uptrend fueled by those earlier purchases.

A protective stop sits just below the base to limit losses should the breakout fail.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Volume, Price
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

