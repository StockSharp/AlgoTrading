# Renko-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn ein bullischer Renko-Baustein auf einen bärischen folgt, und short bei der entgegengesetzten Änderung.

## Details

- **Einstiegskriterien**:
  - **Long**: bullischer Renko-Baustein nach einem bärischen.
  - **Short**: bärischer Renko-Baustein nach einem bullischen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CandleType` = Renko.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Renko
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Renko
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
