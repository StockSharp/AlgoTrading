# Sharpe Ratio Forced Selling — Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Sharpe Ratio Forced Selling Strategie geht long, wenn die rollende Sharpe Ratio unter einen negativen Schwellenwert fällt, und schließt die Position, wenn sie über einen positiven Schwellenwert steigt oder der Haltezeitraum eine Grenze überschreitet. Renditen können über logarithmische oder einfache Änderungen berechnet und um einen risikofreien Zinssatz bereinigt werden.

## Details

- **Einstiegskriterien**: Sharpe Ratio unter `EntrySharpeThreshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Sharpe Ratio über `ExitSharpeThreshold` oder Haltezeitraum überschreitet `MaxHoldingDays`.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Sharpe Ratio
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
