# Larry Connors 3 Day High Low Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implements the Larry Connors 3 Day High/Low mean reversion approach.

## Logic

- Buy when:
  - Close is above the long moving average.
  - Close is below the short moving average.
  - High and low have been lower for three consecutive candles.
- Exit when price closes above the short moving average.

## Parameters

- **Long MA Length** — period for the long SMA (default 200)
- **Short MA Length** — period for the short SMA (default 5)
- **Candle Type** — timeframe used for analysis
