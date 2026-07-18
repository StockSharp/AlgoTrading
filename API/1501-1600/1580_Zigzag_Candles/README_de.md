# Zigzag-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Einfache Strategie, die auf ZigZag-Pivotpunkte reagiert. Eine Long-Position wird eröffnet, wenn ein neuer tiefer Pivot entsteht, während bei neuen hohen Pivots eine Short-Position eingegangen wird.

## Details
- **Einstiegskriterien**: Pivot-Hochs und -Tiefs des ZigZag.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenüberliegender Pivot.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZigzagLength` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
