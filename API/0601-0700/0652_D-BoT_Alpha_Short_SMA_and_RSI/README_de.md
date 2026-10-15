# D-BoT Alpha Short SMA und RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Short-Strategie, die verkauft, wenn der RSI einen Schwellenwert von unten kreuzt, während der Preis unterhalb eines einfachen gleitenden Durchschnitts bleibt. Ein Trailing Stop folgt neuen Tiefs und Positionen werden geschlossen, wenn der RSI Stop- oder Take-Profit-Niveaus erreicht.

## Details

- **Einstiegskriterien**: RSI kreuzt das Einstiegsniveau von unten und der Preis liegt unterhalb der SMA.
- **Ausstiegskriterien**: Preis kreuzt den Trailing Stop von unten oder RSI erreicht Stop- oder Take-Profit-Niveaus.
