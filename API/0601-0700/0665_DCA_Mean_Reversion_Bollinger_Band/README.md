# DCA Mean Reversion Bollinger Band Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Buys a fixed dollar amount when price crosses below the lower Bollinger Band or on the first day of each month. All positions are closed on a specified date.

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
