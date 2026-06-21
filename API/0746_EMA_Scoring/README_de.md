# EMA-Scoring-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie bewertet die Marktrichtung anhand von drei EMA-Linien und handelt, wenn ein Punkteschwellenwert überschritten wird.

## Details
- **Einstiegskriterien**:
  - **Long**: Punkte kreuzen über den Schwellenwert.
  - **Short**: Punkte kreuzen unter den negativen Schwellenwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Short EMA Period` = 21
  - `Medium EMA Period` = 50
  - `Long EMA Period` = 100
  - `Score Threshold` = 4
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Mittelfristig
