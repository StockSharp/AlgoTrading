# Quantum Sentiment Flux-Strategie (Anfänger)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn der schnelle EMA den langsamen EMA von unten kreuzt und der Unterschied zwischen beiden einen ATR-basierten Schwellenwert überschreitet. Bei umgekehrtem Signal wird Short gegangen. Positionen werden geschlossen, wenn der Preis ein ATR-Vielfaches gegen die Position läuft oder ein Gewinnziel von zwei ATR-Vielfachen erreicht wird. Eine Abkühlperiode begrenzt die Handelsfrequenz.

## Parameter
- Kerzentyp
- Länge des schnellen EMA
- Länge des langsamen EMA
- ATR-Periode
- ATR-Multiplikator
- MA-Stärkeschwelle
- Abkühlungsbalken
- Menge
