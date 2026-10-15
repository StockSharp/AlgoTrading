# Mustang Algo Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen RSI-basierten globalen Stimmungsoszillator verwendet, der mit WMA geglättet wird, um Kanalkreuzungen zu handeln.

## Details

- **Einstiegskriterien**: RSI/WMA-Oszillator-Kreuzungen mit den Grenzen.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Gegensignal oder Stop/Take.
- **Stops**: Prozentbasiert, optional.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: RSI, WMA
  - Stops: Prozent
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
