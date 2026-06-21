# RedK Langsamer Glatt-Durchschnitt RSS WMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet einen dreifachen gewichteten gleitenden Durchschnitt zur Rauschfilterung. Eine Position wird eröffnet, wenn der geglättete Durchschnitt die Richtung wechselt: Long wenn er nach oben dreht, Short wenn er nach unten dreht.

## Details

- **Einstiegskriterien**:
  - **Long**: Die Steigung des dreifachen WMA dreht nach oben.
  - **Short**: Die Steigung des dreifachen WMA dreht nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenläufiges Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `CombinedSmoothness` = 15
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: WeightedMovingAverage
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
