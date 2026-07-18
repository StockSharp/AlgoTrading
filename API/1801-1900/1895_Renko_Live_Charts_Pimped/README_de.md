# Renko Live Charts Pimped-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie baut Renko-Steine auf und handelt bei Richtungsänderungen. Optional kann die Steingröße aus ATR-Werten berechnet werden, sodass die Renko-Struktur sich an die Marktvolatilität anpassen kann.

## Details

- **Einstiegskriterien**:
  - **Long**: bullischer Renko-Stein nach einem bärischen.
  - **Short**: bärischer Renko-Stein nach einem bullischen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Renko, ATR
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Renko
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
