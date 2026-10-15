# Ttp Intelligenter Akkumulator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Long-Positionen akkumuliert, wenn der RSI eine Standardabweichung unter seinen Mittelwert fällt, und sie verteilt, wenn der RSI über dieselbe Schwelle steigt.

## Details

- **Einstiegskriterien**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) und Gewinn über `MinProfit`
- **Stops**: Nein
- **Standardwerte**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: RSI, MA, StdDev
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
