# Normalisierte Oszillatoren Spinnennetz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet mehrere Oszillatoren (RSI, Stochastic, Correlation, Money Flow Index, Williams %R, Percent Up, Chande Momentum Oscillator und Aroon Oscillator). Alle Werte werden in den Bereich 0-1 normalisiert und gemittelt, um Handelssignale zu erzeugen. Die Strategie kauft, wenn der Durchschnitt 0,6 überschreitet, und geht short, wenn er unter 0,4 fällt.

## Eingaben
- **Length** — Rückblickperiode für alle Oszillatoren
- **Candle type** — Zeitrahmen der verwendeten Kerzen

## Hinweise
Dies ist ein vereinfachter Port des TradingView-Skripts „Normalized Oscillators Spider Chart [LuxAlgo]", das die Verwendung von Indikatoren in StockSharp demonstriert.
