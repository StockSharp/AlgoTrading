# Bollinger EMA Stats-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei Bollinger-Band-Sets zur Definition von Einstiegs- und Stop-Zonen sowie eine EMA als Ausstiegsziel.

## Details
- **Einstiegskriterien**:
  - **Long**: Close < unteres Bollinger Band (Eintrittsmultiplikator).
  - **Short**: Close > oberes Bollinger Band (Eintrittsmultiplikator).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gewinnziel bei der EMA.
  - Stop-Loss beim breiteren Bollinger Band.
- **Stops**: Ja.
- **Standardwerte**:
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, EMA
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Mittelfristig
