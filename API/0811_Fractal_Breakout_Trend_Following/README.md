# Fractal Breakout Trend Following
[Русский](README_ru.md) | [中文](README_cn.md)

Fractal Breakout Trend Following enters on a buy stop above an activated bullish fractal when volatility is low.

## Details

- **Entry Criteria**: Up fractal above Alligator teeth and averaged ATR percentile below threshold; buy stop at fractal level.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss at the higher of percent stop or down fractal activation.
- **Stops**: Yes.
- **Default Values**:
  - `StopLossPercent` = 0.03
  - `AtrThreshold` = 50
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromHours(1)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filters**:
  - Category: Trend Following
  - Direction: Long
  - Indicators: Fractal, SMMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
