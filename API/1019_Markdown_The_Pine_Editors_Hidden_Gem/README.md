# Markdown The Pine Editor's Hidden Gem Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bollinger Bands built on a simple moving average. A long position opens when the price closes above the upper band, and a short position opens when it closes below the lower band.

## Details

- **Entry Criteria**:
  - **Long**: Close price crosses above the upper band.
  - **Short**: Close price crosses below the lower band.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Length` = 50
  - `Multiplier` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
