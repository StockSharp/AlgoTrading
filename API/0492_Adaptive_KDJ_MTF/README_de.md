# Adaptiver KDJ (MTF)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die adaptive KDJ-Strategie kombiniert KDJ-Oszillatorwerte aus drei Zeitrahmen. Jeder Zeitrahmen wird mit einer EMA geglättet und mithilfe einstellbarer Gewichte kombiniert. Die Trendstärke wird mit einer SMA des kombinierten Oszillators gemessen, der die Überkauf- und Überverkauft-Niveaus anpasst.

Die Strategie geht Long, wenn die J-Linie unter dem adaptiven Kaufniveau liegt und die K-Linie die D-Linie nach oben kreuzt. Sie geht Short, wenn die J-Linie über dem adaptiven Verkaufsniveau liegt und die K-Linie die D-Linie nach unten kreuzt.

## Details

- **Einstiegskriterien**: KDJ-Kreuzung mit J unterhalb/oberhalb dynamischer Niveaus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic, EMA, SMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
