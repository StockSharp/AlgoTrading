# Estrategia de Canal Keltner por Kevin Davey
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sencillo sistema de canal de volatilidad. Compra cuando el cierre cae por debajo de la banda inferior del Canal Keltner y vende en corto cuando el cierre sube por encima de la banda superior. El canal se construye a partir de una EMA y un múltiplo de ATR.

## Parámetros predeterminados
- `EmaPeriod` = 10
- `AtrPeriod` = 14
- `AtrMultiplier` = 1.6
- `CandleType` = 5 minute
