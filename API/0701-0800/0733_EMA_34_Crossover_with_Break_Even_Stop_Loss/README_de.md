# EMA-34-Crossover-Strategie mit Break-Even-Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **EMA 34 Crossover with Break Even Stop Loss** geht long, wenn der Preis die 34-Perioden-EMA nach oben kreuzt. Der Stop Loss wird am Tief der vorherigen Kerze platziert, Take Profit liegt beim Zehnfachen des Risikos, und der Stop wird auf Break Even gesetzt, nachdem der Preis das Dreifache des Risikos erreicht.

## Details
- **Einstiegskriterien**: Schlusskurs kreuzt EMA(34) von unten nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop Loss am vorherigen Tief oder Take Profit bei 10× Risiko.
- **Stops**: Ja, Break-Even-Stop.
- **Standardwerte**:
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
