# Bollinger and Stochastic Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when the price closes below the lower Bollinger Band and the Stochastic %K is below 20. It enters short when the price closes above the upper band and %K is above 80. An ATR-based trailing stop protects open positions.

## Details
- **Entry Criteria:**
  - **Long:** close < lower Bollinger Band and %K < 20.
  - **Short:** close > upper Bollinger Band and %K > 80.
- **Long/Short:** both.
- **Exit Criteria:** ATR trailing stop.
- **Stops:** trailing stop based on ATR * multiplier.
- **Default Values:** Bollinger length = 20, deviation = 2, Stochastic length = 14, smoothing = 3, ATR period = 14, ATR multiplier = 1.5.
