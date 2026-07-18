# Color Code Overlay Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades on candle color changes using a custom color code calculation with fixed pip-based stops.

## Logic
- Builds custom color code candles from OHLC values.
- Detects color switches when body exceeds 1% of the candle range.
- Goes long on red-to-green, short on green-to-red according to trade type.
- Operates only between `StartTime` and `EndTime`.
- Applies `StopLossPips` and `TakeProfitPips` protections.
