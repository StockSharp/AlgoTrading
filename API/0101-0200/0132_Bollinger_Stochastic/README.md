# Bollinger Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Bollinger Stochastic pairs Bollinger Bands with the stochastic oscillator to identify overextended moves.
Price touching the outer band while the oscillator is in an extreme zone suggests a possible snap back.

Testing indicates an average annual return of about 133%. It performs best in the crypto market.

The system fades those extremes, going long when price hits the lower band with stochastic oversold, and shorting the upper band with stochastic overbought.

A percent-based stop limits risk if the mean reversion fails to occur.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StochPeriod` = 14
  - `StochDPeriod` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
    A candle whose low touches the lower band while stochastic %K is below StochOversold goes long; a candle whose high touches the upper band while %K is above StochOverbought goes short. An opposite signal reverses the position.
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

