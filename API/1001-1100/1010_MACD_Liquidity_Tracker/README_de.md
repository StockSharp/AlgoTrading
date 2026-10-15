# MACD Liquiditäts-Tracker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MACD Liquidity Tracker verwendet MACD-Farbzustände zur Erzeugung von Handelssignalen. Vier Modi (Fast, Normal, Safe, Crossover) passen die Signalempfindlichkeit an. Optionaler Stop-Loss und Take-Profit werden unterstützt.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Abhängig von `SystemType` (Standard `Normal` verwendet MACD über der Signallinie).
  - **Short**: Abhängig von `SystemType` (Standard `Normal` verwendet MACD unter der Signallinie).
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Optionaler Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long/Short
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
