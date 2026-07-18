# Divergenz-Strategie für viele Indikatoren
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt bullische und bärische Divergenzen zwischen Preis und RSI sowie MACD-Histogramm. Wenn die Anzahl der Divergenzen den angegebenen Schwellenwert erreicht, eröffnet die Strategie einen Trade in die entgegengesetzte Richtung.

## Parameter
- `RsiPeriod` – Periode für die RSI-Berechnung.
- `MacdFastPeriod` – schnelle Periode für den MACD.
- `MacdSlowPeriod` – langsame Periode für den MACD.
- `MacdSignalPeriod` – Signalperiode für den MACD.
- `MinDivergence` – Mindestanzahl von Indikatoren, die eine Divergenz bestätigen.
- `CandleType` – Kerzentyp für das Abonnement.
