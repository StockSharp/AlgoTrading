# Stochastic Heat Map-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Stochastic Heat Map-Strategie mittelt eine Reihe von Stochastic-Oszillatoren mit steigenden Perioden.
Der kombinierte Wert wird erneut geglättet, um eine schnelle und eine langsame Linie zu bilden.
Trades gehen long, wenn die schnelle Linie die langsame nach oben kreuzt, und short beim umgekehrten Kreuzungspunkt.

## Details

- **Einstiegskriterien**: schnell/langsam-Kreuzung
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `Increment` = 10
  - `SmoothFast` = 2
  - `SmoothSlow` = 21
  - `PlotNumber` = 28
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
