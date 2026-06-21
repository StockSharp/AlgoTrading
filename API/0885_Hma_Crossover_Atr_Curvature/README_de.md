# HMA Crossover ATR Curvature
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

HMA Crossover ATR Curvature ist eine Trendfolge-Strategie, die einen schnellen und langsamen Hull Moving Average Crossover mit einem Krümmungsfilter kombiniert. Die Positionsgröße basiert auf ATR und dem Risikoanteil, und Trades werden durch einen ATR-basierten Trailing-Stop geschützt.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schnelle HMA kreuzt langsame HMA von unten und Krümmung liegt über dem Schwellenwert.
  - **Short**: Schnelle HMA kreuzt langsame HMA von oben und Krümmung liegt unter dem negativen Schwellenwert.
- **Ausstiegskriterien**: ATR Trailing-Stop.
- **Stops**: ATR Trailing-Stop.
- **Standardwerte**:
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: HMA, ATR
  - Komplexität: Niedrig
  - Risikolevel: Mittel
