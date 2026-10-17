# DCA Mean Reversion Bollinger Band Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Buys a fixed dollar amount when price crosses below the lower Bollinger Band or on the first day of each month. All positions are closed on a specified date.

### Exact C#/Python trade contract

Default parameters: `InvestmentAmount=200` quote-currency units, `OpenDate=DateTime.MinValue`, `CloseDate=DateTime.MaxValue`, `StrategyMode=Combined`, `BollingerPeriod=20`, `BollingerMultiplier=2`, `CandleType=5 minutes`. On **finished candles**, calculate Bollinger lower band using SMA20 minus 2 standard deviations. The Bollinger buy occurs when the previous finished `Close >= previous lower band` and the current `Close < current lower band`. Monthly buy occurs **once per calendar month**, on the first available eligible finished candle after indicator warmup. Modes: `Bollinger` (crossings only), `Monthly` (one buy/month), `Combined` (either; one order if both occur together). Between `OpenDate <= date < CloseDate`, each signal buys `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep`, subject to instrument min/max volume. Buys add to the held long; the strategy **never shorts**. At or after `CloseDate`, on the first trading candle it sells the **entire remaining positive position**, and makes no new buys. No stop-loss, short signal or EMA crossover; price slippage can affect the executed quote value. The original README did not specify numerical defaults; they are explicit implementation choices.



## Parameters
- `InvestmentAmount` - amount invested each time
- `OpenDate` - start date for buying
- `CloseDate` - date to close all positions
- `StrategyMode` - BB mean reversion, monthly DCA or combined
- `BollingerPeriod` - Bollinger Bands period
- `BollingerMultiplier` - standard deviation multiplier
- `CandleType` - timeframe for Bollinger calculation

## Indicators
- Bollinger Bands
