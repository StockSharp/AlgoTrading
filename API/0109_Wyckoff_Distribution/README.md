# Wyckoff Distribution Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Wyckoff Distribution is a topping phase characterized by heavy selling into rallies and tests of resistance.
Volume often expands on down moves and contracts on bounces, suggesting large interests are liquidating positions.

This strategy sells short when price breaks down from the distribution range, anticipating a sustained decline.

A stop just above the range protects against false breakouts, and positions close if price returns to the top of the structure.

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
