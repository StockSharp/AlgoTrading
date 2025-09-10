# Squeeze Pro Overlays Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Squeeze Pro Overlays strategy detects volatility contraction when Bollinger Bands are completely inside multiple Keltner Channels. Once the squeeze releases, the slope of a linear regression on closing prices determines trade direction.

## Details

- **Entry Criteria**:
  - Squeeze ends (Bollinger Bands move outside the widest Keltner Channel).
  - **Long**: Momentum slope > 0.
  - **Short**: Momentum slope < 0.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `SqueezeLength` = 20
- **Filters**:
  - Category: Volatility breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Keltner Channels, Linear Regression
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
