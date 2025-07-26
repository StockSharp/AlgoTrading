# Bollinger Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Bollinger Stochastic pairs Bollinger Bands with the stochastic oscillator to identify overextended moves.
Price touching the outer band while the oscillator is in an extreme zone suggests a possible snap back.

The system fades those extremes, going long when price hits the lower band with stochastic oversold, and shorting the upper band with stochastic overbought.

A percent-based stop limits risk if the mean reversion fails to occur.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 133%. It performs best in the crypto market.
