# Spectral RVI Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Spectral RVI Crossover-Strategie glättet den Relative Vigor Index und seine Signallinie und handelt bei deren Kreuzungen.
Sie kauft, wenn der geglättete RVI über die geglättete Signallinie kreuzt, und verkauft bei der entgegengesetzten Kreuzung.

## Details

- **Einstiegskriterien**: geglätteter RVI kreuzt seine geglättete Signallinie
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzte Kreuzung
- **Stops**: Nein
- **Standardwerte**:
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RVI, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 4 Stunden
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
