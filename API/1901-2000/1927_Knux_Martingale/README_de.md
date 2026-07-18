# Knux-Martingal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Martingal-Strategie, die das Handelsvolumen nach einer verlierenden Position erhöht. Die Methode filtert Einstiege nach dem Average Directional Index (ADX), um nur in Trendmärkten zu handeln. Bullische Kerzen eröffnen Long-Positionen, bärische Kerzen eröffnen Short-Positionen.

## Details

- **Einstiegskriterien**:
  - ADX > 25
  - Long: `Close > Open`
  - Short: `Close < Open`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge, Martingal
  - Richtung: Beide
  - Indikatoren: AverageDirectionalIndex
  - Stops: Absolut
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
