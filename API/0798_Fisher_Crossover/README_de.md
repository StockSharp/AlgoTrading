# Fisher-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Fisher Transform Indikator, um Long-Positionen einzugehen, wenn der Indikator seinen vorherigen Wert nach oben kreuzt und dabei unter 1 liegt. Positionen werden geschlossen, wenn der Indikator seinen vorherigen Wert nach unten kreuzt und dabei über 1 liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Fisher crosses above previous Fisher` && `Fisher < 1`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - `Fisher crosses below previous Fisher` && `Fisher > 1`
- **Stops**: Nein
- **Standardwerte**:
  - `Fisher Length` = 9
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Fisher Transform
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
