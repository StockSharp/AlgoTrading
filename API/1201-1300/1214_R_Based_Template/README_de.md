# R-basierte Strategie-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RSI-basierte Strategie mit risikomanagierter Positionsgröße und konfigurierbaren Stop-Typen.

## Details

- **Einstiegskriterien**:
  - Long, wenn RSI unter `OversoldLevel` kreuzt.
  - Short, wenn RSI über `OverboughtLevel` kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit mit dem Vielfachen `TpRValue`.
- **Stops**:
  - Fixed, Atr, Percentage oder Ticks.
- **Standardwerte**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Variable
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
