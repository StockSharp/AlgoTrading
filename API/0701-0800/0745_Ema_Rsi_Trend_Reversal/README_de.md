# EMA RSI Trendumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die bei einem EMA-Crossover mit RSI-Bestätigung Long geht und bei einem gegenteiligen Crossover mit RSI unterhalb des Levels aussteigt. Verwendet prozentualen Take-Profit und Stop-Loss.

## Details

- **Einstiegskriterien**:
  - Long: `FastEMA crosses above SlowEMA && RSI > RsiLevel`
- **Long/Short**: Nur Long
- **Stops**: Prozentualer Take-Profit und Stop-Loss
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: EMA, RSI
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
