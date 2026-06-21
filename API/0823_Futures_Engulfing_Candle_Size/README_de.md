# Futures-Strategie mit Engulfing-Kerzengröße
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt einmal täglich, wenn die Spanne einer Kerze innerhalb eines ausgewählten Zeitfensters einen Tick-Schwellenwert überschreitet. Die Richtung folgt dem Kerzenkörper, der Ausstieg erfolgt per Take Profit und Stop Loss.

## Details

- **Einstiegskriterien**: Kerzenspanne in Ticks innerhalb der Handelssitzung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Take Profit oder Stop Loss.
- **Stops**: Take Profit und Stop Loss.
- **Standardwerte**:
  - `CandleType` = 1 minute
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
