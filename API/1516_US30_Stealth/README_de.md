# US30 Stealth-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Price-Action-Strategie für US30, die den gleitenden Durchschnitt Steigung, Engulfing-Muster, Volumen und Session-Filter verwendet.
Die Positionsgröße wird aus dem Risiko pro Trade berechnet, mit Stop-Loss und Take-Profit auf Basis der Kerzenspanne.

## Details

- **Einstiegskriterien**: Trendrichtung, drei niedrigere Hochs oder höhere Tiefs, Engulfing-Muster, Volumen- und Zeitfilter.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss
- **Stops**: Fest
- **Standardwerte**:
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
