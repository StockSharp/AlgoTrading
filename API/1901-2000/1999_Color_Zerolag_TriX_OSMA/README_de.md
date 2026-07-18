# Color Zerolag TriX OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie verwendet einen Zero-Lag-TRIX-OSMA-Oszillator, der aus fünf verschiedenen TRIX-Perioden aufgebaut ist. Jede TRIX-Komponente wird gewichtet und geglättet, um einen einzelnen Oszillator zu bilden, der mit minimalem Lag auf Trendänderungen reagiert. Eine Long-Position wird eröffnet, wenn der Oszillator nach oben dreht, und eine Short-Position, wenn er nach unten dreht.

## Funktionsweise

1. Fünf TRIX-Werte werden mit dreifachen exponentiellen gleitenden Durchschnitten und der Rate of Change berechnet.
2. Die TRIX-Werte werden mit ihren Gewichten kombiniert, um einen schnellen Trendwert zu bilden.
3. Der schnelle Trend wird zweimal geglättet, um einen Zero-Lag-OSMA-Oszillator zu erzeugen.
4. Trendumkehrungen werden durch Vergleich der letzten zwei Oszillatorwerte erkannt.
5. Bei einem Aufwärtsdreh wird Long und bei einem Abwärtsdreh Short eingegangen; bestehende entgegengesetzte Positionen werden vor dem Eröffnen einer neuen geschlossen.

## Parameter

- `Smoothing1` – Glättungsfaktor für den langsamen Trend.
- `Smoothing2` – Glättungsfaktor für die OSMA-Linie.
- `Factor1..Factor5` – Gewichte für jede TRIX-Komponente.
- `Period1..Period5` – Perioden für die fünf TRIX-Berechnungen.
- `CandleType` – Kerzenserie für Berechnungen.

## Indikatoren

- TripleExponentialMovingAverage
- RateOfChange
- Benutzerdefinierte Zero-Lag-TRIX-OSMA-Kombination

## Hinweise

Die Strategie erfordert, dass alle fünf TRIX-Indikatoren gebildet sind, bevor Signale erzeugt werden. Der Schutz für Stops und Ziele wird über `StartProtection` aktiviert.
