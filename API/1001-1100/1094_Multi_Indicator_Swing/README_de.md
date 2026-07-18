# Multi-Indikator-Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Swing-Strategie, die Parabolic SAR, SuperTrend, ADX und Volumen-Delta-Bestätigung kombiniert.

## Details

- **Einstiegskriterien**: Alle aktivierten Indikatoren stimmen überein.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Erreichen von Stop-Loss/Take-Profit.
- **Stops**: Optionale prozentuale Niveaus.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: PSAR, SuperTrend, ADX, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (2m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
