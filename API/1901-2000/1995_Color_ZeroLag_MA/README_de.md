# Color Zero Lag MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen Zero Lag Moving Average (ZLMA), um Trendumkehrungen zu erkennen. Sie eröffnet Long-Positionen, wenn die ZLMA nach oben dreht, und Short-Positionen, wenn die ZLMA nach unten dreht. Bestehende Positionen werden geschlossen, wenn sich die Steigung des Indikators umkehrt.

## Parameter

- **Length**: Periode des Zero Lag Moving Average.
- **Candle Type**: Zeitrahmen für die von der Strategie verwendeten Kerzen.
- **Open Buy**: Eröffnen von Long-Positionen aktivieren.
- **Open Sell**: Eröffnen von Short-Positionen aktivieren.
- **Close Buy**: Long-Positionen schließen, wenn die ZLMA nach unten dreht.
- **Close Sell**: Short-Positionen schließen, wenn die ZLMA nach oben dreht.

## Logik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. Den Zero Lag Moving Average berechnen.
3. Die letzten zwei ZLMA-Werte verfolgen, um die Steigungsrichtung zu bestimmen.
4. Wenn die Steigung von abwärts zu aufwärts wechselt, Short-Positionen schließen und eine Long-Position eröffnen.
5. Wenn die Steigung von aufwärts zu abwärts wechselt, Long-Positionen schließen und eine Short-Position eröffnen.

Dieser einfache Ansatz folgt dem Farbwechsel des Zero Lag Moving Average, um potenzielle Trendumkehrungen zu erfassen.
