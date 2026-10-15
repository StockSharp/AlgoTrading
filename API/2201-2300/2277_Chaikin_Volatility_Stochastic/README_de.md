# Chaikin-Volatilität-Stochastik-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet einen stochastischen Oszillator auf Chaikin-Volatilitätswerte an, um Trendumkehrungen zu erfassen. Der Hoch-Tief-Bereich jeder Kerze wird mit einer EMA geglättet, dann mit einer stochastischen Berechnung normalisiert und schließlich mit einem gewichteten gleitenden Durchschnitt geglättet.

Wenn der geglättete Oszillator nach einem Anstieg abwärts dreht, wird eine Long-Position eröffnet und jede Short-Position geschlossen. Wenn der Oszillator nach einem Rückgang aufwärts dreht, wird eine Short-Position eröffnet und jede Long-Position geschlossen.

## Parameter
- **Candle Type**: Zeitrahmen für die Kerzen-Subscription.
- **EMA Length**: Glättungsperiode für den Hoch-Tief-Bereich.
- **Stochastic Length**: Rückblickperiode für die stochastische Berechnung.
- **WMA Length**: Gewichteter gleitender Durchschnitt zur Glättung des Oszillators.
- **Enable Longs / Enable Shorts**: Erlaubte Handelsrichtungen umschalten.

## Indikatoren
- ExponentialMovingAverage
- Highest und Lowest
- WeightedMovingAverage

## Handelsregeln
- **Long-Einstieg**: Oszillator stieg und dreht abwärts.
- **Short-Einstieg**: Oszillator fiel und dreht aufwärts.
- Entgegengesetzte Positionen werden bei Signalwechsel geschlossen.
