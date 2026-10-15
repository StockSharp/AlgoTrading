# Erweiterte MACD-MTF-Strategie mit Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Zeitrahmen-Strategie, die MACD-basiertes Scoring und eine ATR-abgeleitete Trailing-Stop-Linie verwendet.

## Details

- **Einstiegskriterien**: MACD-Score wird positiv oder negativ.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Durchbruch der Trailing-Stop-Linie.
- **Stops**: ATR Trailing Stop.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
