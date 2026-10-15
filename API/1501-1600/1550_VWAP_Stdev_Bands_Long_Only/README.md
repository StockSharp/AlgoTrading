# VWAP Stdev Bands Strategy (Long Only)
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Buys when price crosses below the lower VWAP standard deviation band and closes on reaching the profit target.

## Parameters

- **DevUp**: Standard deviation multiplier above VWAP.
- **DevDown**: Standard deviation multiplier below VWAP.
- **ProfitTarget**: Profit target in price units.
- **GapMinutes**: Gap before new order in minutes.
- **CandleType**: Type of candles.

