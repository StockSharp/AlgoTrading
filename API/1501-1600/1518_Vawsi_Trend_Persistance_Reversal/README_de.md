# VAWSI und Trendpersistenz-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umkehr-Strategie, die VAWSI, Trendpersistenz und ATR kombiniert, um eine dynamische Schwelle auf Heikin-Ashi-Kerzen aufzubauen.

## Details

- **Einstiegskriterien**: Heikin-Ashi-Schluss kreuzt die dynamische Schwelle nach oben/unten
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenseitige Kreuzung oder Schutz-Stops
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI, ATR
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
