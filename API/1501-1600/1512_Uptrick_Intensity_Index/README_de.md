# Uptrick Intensity Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet den Trendintensitätsindex aus drei gleitenden Durchschnitten und handelt bei Crossovern von TII und seinem eigenen gleitenden Durchschnitt.

## Details

- **Einstiegskriterien**: TII kreuzt seine SMA nach oben (Kauf) oder nach unten (Verkauf)
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `Ma3Length` = 50
  - `TiiMaLength` = 50
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, TII
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
