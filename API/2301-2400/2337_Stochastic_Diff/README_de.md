# Stochastic-Differenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Differenz zwischen den %K- und %D-Linien des Stochastic-Oszillators. Die Differenz wird mit einem exponentiellen gleitenden Durchschnitt geglättet, um Rauschen zu reduzieren. Eine Long-Position wird eröffnet, wenn die geglättete Differenz ein lokales Tief bildet und nach oben dreht. Eine Short-Position wird eröffnet, wenn die geglättete Differenz ein lokales Hoch bildet und nach unten dreht.

## Funktionsweise

1. Berechnung von Stochastic %K und %D mit benutzerdefinierten Perioden.
2. Berechnung der Differenz `%K - %D` und Glättung mit einer EMA.
3. Erkennung von Wendepunkten in der geglätteten Differenz:
   - Wenn der Wert fiel und dann steigt, wird eine Long-Position eröffnet.
   - Wenn der Wert stieg und dann fällt, wird eine Short-Position eröffnet.
4. Optionale Stop-Loss- und Take-Profit-Absicherungen in Prozent anwenden.

## Parameter

| Name | Beschreibung |
| --- | --- |
| Candle Type | Kerzentyp für die Berechnungen |
| %K Period | Periode für die %K-Linie |
| %D Period | Periode für die %D-Linie |
| Slowing | Zusätzliche Glättung von %K |
| Smoothing Length | EMA-Länge für die Differenz |
| Stop Loss % | Stop-Loss-Größe in Prozent |
| Take Profit % | Take-Profit-Größe in Prozent |

## Hinweise

- Funktioniert mit jedem Instrument und Zeitrahmen, der vom Datenfeed unterstützt wird.
- Für Bildungszwecke entwickelt, um indikatorbasierte Einstiegssignale zu demonstrieren.
