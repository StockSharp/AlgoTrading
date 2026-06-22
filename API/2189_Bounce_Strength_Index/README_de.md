# Bounce-Strength-Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert eine vereinfachte Version des Bounce Strength Index (BSI). Der Indikator misst, wie der Kurs innerhalb einer jüngsten Spanne schließt, und wendet eine doppelte Glättung an, um Momentum-Wechsel hervorzuheben.

## Logik
- Berechnung der jüngsten Höchst- und Tiefstkurse mit den Indikatoren **Highest** und **Lowest**.
- Bestimmung der Schlusskursposition innerhalb dieser Spanne und zweimalige Glättung des Ergebnisses mit **SimpleMovingAverage**.
- Wenn der Indikator nach oben dreht, werden Short-Positionen geschlossen und eine Long-Position eröffnet.
- Wenn der Indikator nach unten dreht, werden Long-Positionen geschlossen und eine Short-Position eröffnet.

## Parameter
- `CandleType` – Kerzenserie für die Analyse.
- `RangePeriod` – Lookback-Periode für die Berechnung der Spanne.
- `Slowing` – Länge der schnellen Glättung.
- `AvgPeriod` – Länge der langsamen Glättung.

## Indikatoren
- BounceStrengthIndex (benutzerdefiniert)
- Highest
- Lowest
- SimpleMovingAverage
