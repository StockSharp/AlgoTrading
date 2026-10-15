# Strategie Gestriges Hoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long-Ausbruch-Strategie, die eine Buy-Stop-Order über dem Hoch des Vortages platziert.
Optionaler ROC-Filter, Trailing-Stop und EMA-Schlusskurs bieten zusätzliche Risikokontrolle.

## Details

- **Einstiegskriterien**: Schlusskurs unter dem gestrigen Hoch, dann Buy-Stop bei Hoch + Gap
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Stop-Loss, Take-Profit, optionaler Trailing-Stop oder EMA-Kreuzung
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: Price, ROC, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
