# Z-Score-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie berechnet den Z-Score einer Heikin-Ashi EMA und handelt anhand von Kreuzungen dynamischer Schwellenwerte, die aus jüngsten Ranges abgeleitet werden.

## Details

- **Einstiegskriterien**: Score kreuzt über das jüngste Tief oder EMA des Score kreuzt über die Mitte der Range
- **Long/Short**: Beide
- **Ausstiegskriterien**: EMA des Score kreuzt unter das jüngste Hoch oder Tief
- **Stops**: Nein
- **Standardwerte**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: EMA, SMA, StdDev, Highest, Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
