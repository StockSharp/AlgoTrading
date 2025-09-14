# Auto Trade With Bollinger Bands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bollinger Bands, RSI and Stochastic oscillator to automatically open trades during a specified GMT time window. A short position is opened when the previous candle closes above the upper Bollinger Band while RSI is above 75 and Stochastic %K is above 85. A long position is opened when the candle closes below the lower Bollinger Band with RSI below 25 and Stochastic %K below 155. Only one position per direction is allowed. A trailing stop in points protects open positions.

## Parameters

- `OpenBuy` – enable opening long positions.
- `OpenSell` – enable opening short positions.
- `GmtTradeStart` – trading start hour in GMT (exclusive).
- `GmtTradeStop` – trading stop hour in GMT (exclusive).
- `BbPeriod` – period for Bollinger Bands.
- `RsiPeriod` – period for RSI indicator.
- `StochKPeriod` – %K period for Stochastic oscillator.
- `StochDPeriod` – %D period for Stochastic oscillator.
- `StochSlowing` – smoothing factor for Stochastic oscillator.
- `TrailingStop` – trailing stop distance in points.
- `CandleType` – candle timeframe used for calculations.
