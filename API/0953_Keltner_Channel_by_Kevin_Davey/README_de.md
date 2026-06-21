# Keltner Channel-Strategie von Kevin Davey
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein einfaches Volatilitätskanal-System. Es kauft, wenn der Schlusskurs unter das untere Keltner-Band fällt, und geht short, wenn der Schlusskurs über das obere Band steigt. Der Kanal wird aus einem EMA und einem ATR-Vielfachen gebildet.

## Standardparameter
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 minute
