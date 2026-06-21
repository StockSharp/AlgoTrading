# RSI Zyklisch Geglättet Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem zyklisch geglätteten RSI-Indikator. Sie berechnet dynamische Perzentilbänder und handelt Umkehrungen, wenn der Oszillator diese kreuzt.

## Details

- **Einstiegskriterien**: CRSI kreuzt das untere Band nach oben oder das obere Band nach unten.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Kreuzung des gegenüberliegenden Bandes.
- **Stops**: Ja.
- **Standardwerte**:
  - `DominantCycleLength` = 20
  - `Vibration` = 10
  - `Leveling` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
