# BB RSI Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

Combines Bollinger Bands with RSI momentum and protects trades with a conditional trailing stop.
Longs occur when price pierces the lower band and RSI exits oversold. Shorts trigger on the upper band with overbought RSI.

The stop-loss starts at a fixed distance and converts to a trailing stop once price moves favorably by a preset offset.

## Details

- **Entry Criteria**: Bollinger Band breakout with RSI confirmation
- **Long/Short**: Both
- **Exit Criteria**: Initial stop-loss or trailing stop
- **Stops**: Yes, dynamic trailing
- **Default Values**:
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, RSI
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
