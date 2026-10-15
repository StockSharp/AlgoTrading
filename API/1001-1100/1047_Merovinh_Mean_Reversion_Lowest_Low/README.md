# Merovinh - Mean Reversion Lowest Low
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy buys when the current lowest low of a lookback period breaks successive previous lows a configurable number of times. It closes the position once a new highest high appears within the same period.

## Parameters
- Bars — lookback length for highest/lowest.
- Number Of Lows — required consecutive broken lows to enter.
- Start Date / End Date — trading range.
- Candle Type — type of candles.
