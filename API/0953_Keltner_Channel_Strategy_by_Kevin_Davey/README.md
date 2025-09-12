# Keltner Channel Strategy by Kevin Davey
[Русский](README_ru.md) | [中文](README_cn.md)

A simple volatility channel system. It buys when the close falls below the lower Keltner band and sells short when the close rises above the upper band. The channel is built from an EMA and an ATR multiple.

## Default Parameters
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 minute
