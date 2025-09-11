# Keltner Channel Strategy by Kevin Davey
[English](README.md) | [中文](README_cn.md)

Простая стратегия на канале Келтнера. Покупает, когда цена закрывается ниже нижней границы, и продаёт, когда закрытие выше верхней границы. Канал строится по EMA и ATR с множителем.

## Параметры по умолчанию
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 минут
