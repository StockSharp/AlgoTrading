# NAS100 und Gold Smart Scalping PRO Enhanced v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie scalpt kurzfristige Bewegungen mit EMA9 und VWAP als dynamische Orientierungslinien, RSI für Momentum und ATR für das Risikomanagement. Ein 15-Minuten-EMA200-Trendfilter hält Trades in Richtung des vorherrschenden Trends, während ein Volumenpike-Filter starke Kerzen sucht. Positionsgrößen werden risikobasiert berechnet, optionale Trailing-Stops und Abkühlzeiten zwischen Trades werden unterstützt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder Gegensignal
- **Stops**: Ja, ATR-basiert
- **Standardwerte**:
  - `CandleType` = 1 minute
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: EMA, VWAP, RSI, ATR, EMA200
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
