# Klassische nackte Z-Score-Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt die Spread zwischen zwei Assets mittels Z-Score. Wenn der Z-Score des Spreads über einen positiven Schwellenwert steigt, verkauft die Strategie das erste Asset und kauft das zweite. Wenn der Z-Score unter den negativen Schwellenwert fällt, kauft sie das erste Asset und verkauft das zweite. Positionen werden geschlossen, wenn der Z-Score gegen null revertiert.

## Parameter
- Kerzentyp
- Lookback-Periode
- Z-Score-Schwellenwert
- Zweites Wertpapier
