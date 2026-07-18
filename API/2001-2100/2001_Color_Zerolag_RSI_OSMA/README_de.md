# Color Zerolag RSI OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen zusammengesetzten Oszillator, der aus fünf RSI-Berechnungen mit unterschiedlichen Perioden aufgebaut ist. Die gewichtete Summe der RSI-Werte wird zweimal geglättet, um eine Zero-Lag-OSMA-Linie zu erzeugen.

## Funktionsweise

1. Fünf RSI-Werte mit den Perioden 8, 21, 34, 55 und 89 berechnen.
2. Jeden RSI mit seinem Gewicht multiplizieren und die Ergebnisse summieren.
3. Zwei Glättungsschritte auf die Summe anwenden, um den OSMA-Wert zu erhalten.
4. Wenn der OSMA nach oben dreht (vorheriger Wert war niedriger als vor zwei Balken und aktueller Wert übersteigt den vorherigen), schließt die Strategie Short-Positionen und öffnet optional eine Long-Position.
5. Wenn der OSMA nach unten dreht (vorheriger Wert war höher als vor zwei Balken und aktueller Wert fällt unter den vorherigen), schließt die Strategie Long-Positionen und öffnet optional eine Short-Position.

## Parameter

- **Smoothing 1, Smoothing 2** – Längen der Glättungsphasen.
- **Factor 1..5** – Gewichte für jede RSI-Komponente.
- **RSI Period 1..5** – Perioden der RSI-Indikatoren.
- **Allow Buy / Allow Sell** – Eröffnung von Long- oder Short-Positionen aktivieren.
- **Close Long / Close Short** – Bestehende Positionen bei entgegengesetzten Signalen schließen.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen (Standard 4 Stunden).

## Hinweise

Die Strategie operiert nur auf abgeschlossenen Kerzen. Der Positionsschutz wird automatisch gestartet, wenn die Strategie beginnt.
