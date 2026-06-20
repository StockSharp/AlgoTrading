# Three Red / Three Green Strategy with ATR Filter
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Enters long after three consecutive bearish candles if ATR is above its 30-period SMA. Exits after three bullish candles or when maximum trade duration is reached.

## Parameters

- **CandleType**: Type of candles.
- **MaxTradeDuration**: Maximum number of bars to keep an open position.
- **UseGreenExit**: Whether to exit after three green candles.
- **AtrPeriod**: Period for ATR calculation (0 disables the filter).
