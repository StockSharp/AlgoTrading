# SmartAssTrade V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die SmartAssTrade V2-Strategie verwendet das MACD-Histogramm und 20-Perioden-gleitende Durchschnitte über mehrere Zeitrahmen (1m, 5m, 15m, 30m, 60m) in Kombination mit Williams %R- und RSI-Filtern, um Trendmomentum zu erfassen. Ein optionaler Trailing-Stop schützt die Gewinne.

## Details

- **Einstiegskriterien**: Die Mehrheit der Zeitrahmen zeigt steigendes MACD-Histogramm und MA mit WPR/RSI-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Der Preis erreicht Take Profit oder Stop Loss; optionaler Trailing-Stop
- **Stops**: Absoluter Stop Loss und Take Profit mit optionalem Trailing
- **Standardwerte**:
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, SMA, Williams %R, RSI
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen (1m,5m,15m,30m,60m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
