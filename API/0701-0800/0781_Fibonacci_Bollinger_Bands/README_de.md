# Fibonacci- und Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long-Mean-Reversion mit Bollinger-Bändern und Fibonacci-Erweiterung unterhalb der vorherigen Handelsspanne.

Standardwerte: `BollingerPeriod = 20`, `BollingerDeviation = 2`, `FibLookback = 50`, `FibonacciLevel = 0.618`, `CandleType = 1 Minute`. Nur abgeschlossene Kerzen. Bollinger verwendet die letzten 20 Schlusskurse einschließlich Signalkerze und die Populationsstandardabweichung. Die Spanne stammt aus höchstem High und niedrigstem Low der vorherigen 50 abgeschlossenen Kerzen, ohne Signalkerze.

Erweiterung nach unten: `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`. Positive Koeffizienten über 1 wie 1.618 sind zulässig. Ohne Position strikt bei `Close < lowerBand` und `Close > F` kaufen. Long vollständig bei `Close >= upperBand` oder `Close >= previousHigh` schließen. Gleichheit mit F erlaubt keinen Einstieg. Keine Shorts, Stop-Aufträge oder zukünftigen Kerzen.
