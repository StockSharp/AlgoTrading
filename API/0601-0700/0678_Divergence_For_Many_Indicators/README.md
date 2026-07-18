# Divergence For Many Indicators Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detects bullish and bearish divergences between price and RSI and MACD histogram. When the number of divergences reaches the specified threshold, the strategy enters a trade in the opposite direction.

## Parameters
- `RsiPeriod` – period for RSI calculation.
- `MacdFastPeriod` – fast period for MACD.
- `MacdSlowPeriod` – slow period for MACD.
- `MacdSignalPeriod` – signal period for MACD.
- `MinDivergence` – minimum indicators confirming divergence.
- `CandleType` – candle type for subscription.
