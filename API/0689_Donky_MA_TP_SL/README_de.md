# Donky MA TP SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt gleitende Durchschnitt-Kreuzungen mit zwei Take-Profit-Zielen und einem Stop-Loss. Sie geht Long, wenn der schnelle SMA den langsamen SMA nach oben kreuzt, und Short, wenn er ihn nach unten kreuzt. Die Hälfte der Position wird beim ersten Ziel geschlossen, der Rest beim zweiten Ziel oder dem Stop-Loss.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt über den langsamen SMA.
  - **Short**: Schneller SMA kreuzt unter den langsamen SMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Zwei feste Take-Profit-Level oder ein fester Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
